using System;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Websocket.Client;
using Websocket.Client.Models;
#if OPEN_CV
using Emgu.CV;
#endif

public class DonkeyCarClient : IDisposable
{
	private readonly static JsonSerializerOptions jsonOptions = new JsonSerializerOptions
	{
		Converters =
		{
			new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
		}
	};

	private const int DefaultPort = 8887;

	/// <summary>
	/// Returns a string representing the URL to the video feed.
	/// </summary>
	public static string ConstructVideoUrl(string host, int port = DefaultPort)
	{
		return $"http://{host}:{port}/video";
	}

	private readonly WebsocketClient client;

#if OPEN_CV
	private readonly VideoCapture capture;
	private Mat matCache = new Mat();
	private object matCacheLock = new object();
	private bool matCacheReady = false;
	private TaskCompletionSource matCacheTask = null;
#endif

	/// <summary>
	/// Messages received from the car. Usually not anything useful.
	/// </summary>
	public event Action<string> OnMessageReceived;
	/// <summary>
	/// Messages sent to the car. You should know what you've sent.
	/// </summary>
	public event Action<string> OnMessageSent;
	/// <summary>
	/// Disconnection events when loosing the connection.
	/// </summary>
	public event Action<DisconnectionType> OnDisconnect;
	/// <summary>
	/// Reconnection events after having lost the connection.
	/// </summary>
	public event Action<ReconnectionType> OnReconnect;

	/// <summary>
	/// Whether or not the client is running and connected to the server.
	/// </summary>
	public bool IsConnected => client.IsRunning;

	/// <summary>
	/// Instantiates a new DonkeyCarClient targeting a certain endpoint.
	/// </summary>
	public DonkeyCarClient(string host, int port = DefaultPort)
	{
		client = new WebsocketClient(new Uri($"ws://{host}:{port}/wsDrive"));
		client.MessageReceived.Subscribe(HandleMessage);
		client.DisconnectionHappened.Subscribe(HandleDisconnect);
		client.ReconnectionHappened.Subscribe(HandleReconnect);

#if OPEN_CV
		capture = new VideoCapture(ConstructVideoUrl(host, port));
		capture.ImageGrabbed += HandleImageGrabbed;
#endif
	}

	private void HandleMessage(ResponseMessage message) =>
		OnMessageReceived?.Invoke(message.Text);
	private void HandleDisconnect(DisconnectionInfo info) =>
		OnDisconnect?.Invoke(info.Type);
	private void HandleReconnect(ReconnectionInfo info) =>
		OnReconnect?.Invoke(info.Type);

#if OPEN_CV
	private void HandleImageGrabbed(object sender, EventArgs e)
	{
		lock (matCacheLock)
		{
			capture.Retrieve(matCache);
			if (matCacheTask is not null)
			{
				var localMatCacheTask = matCacheTask;
				matCacheTask = null;
				Task.Run(() => localMatCacheTask.SetResult());
			}
			else
			{
				matCacheReady = true;
			}
		}
	}
#endif

	/// <summary>
	/// Starts the connection with the car.
	/// </summary>
	public Task Connect()
	{
#if OPEN_CV
		capture.Start();
#endif

		TaskCompletionSource tcs = new TaskCompletionSource();
		OnDisconnect += OnConnectionError;
		client.Start().ContinueWith(_ => tcs.SetResult());
		tcs.Task.ContinueWith(_ => OnDisconnect -= OnConnectionError);

		return tcs.Task;

		void OnConnectionError(DisconnectionType type) =>
			tcs.SetException(new Exception("Could not connect to the server."));
	}

	/// <summary>
	/// Sends a message with <paramref name="angle"/> and <paramref name="throttle"/> to the car.
	/// <param name="angle">
	/// The angle of the wheels. From -1.0 to 1.0 for full left and full right respectively.
	/// </param>
	/// <param name="throttle">
	/// The throttle of the car. From -1.0 to 1.0 for full reverse and full forward respectively.
	/// </param>
	/// </summary>
	/// <remarks>
	/// Only one message can be sent at a time. Await this call before initiating another one.
	/// </remarks>
	public Task Send(double angle, double throttle) =>
		Send(new DonkeyCarMessage(angle, throttle));

	/// <summary>
	/// Sends a premade <see cref="DonkeyCarMessage"/> to the car.
	/// </summary>
	/// <remarks>
	/// Only one message can be sent at a time. Await this call before initiating another one.
	/// </remarks>
	public async Task Send(DonkeyCarMessage message)
	{
		if (IsConnected)
		{
			string jsonMessage = JsonSerializer.Serialize(message, jsonOptions);
			await client.SendInstant(jsonMessage);
			OnMessageSent?.Invoke(jsonMessage);
		}
		else
		{
			throw new InvalidOperationException("Not connected to the server.");
		}
	}

#if OPEN_CV
	/// <summary>
	/// Fetches the most recent frame from the car, or awaits ones arrival.
	/// </summary>
	/// <param name="frame">The <see cref="Mat"/> object into which the frame will be placed.</param>
	public Task FetchFrame(Mat frame)
	{
		lock (matCacheLock)
		{
			if (matCacheReady)
			{
				matCache.CopyTo(frame);
				matCacheReady = false;
				return Task.CompletedTask;
			}
			else
			{
				matCacheTask = new TaskCompletionSource();
				return matCacheTask.Task.ContinueWith(PopulateFrame);
			}
		}

		void PopulateFrame(Task task)
		{
			if (task.IsFaulted)
			{
				throw task.Exception;
			}
			else
			{
				lock (matCacheLock)
				{
					matCache.CopyTo(frame);
					matCacheReady = false;
				}
			}
		}
	}
#endif

	/// <summary>
	/// Stops the connection with the car.
	/// </summary>
	public Task Disconnect()
	{
#if OPEN_CV
		capture.Stop();
#endif
		return client.Stop(WebSocketCloseStatus.NormalClosure, "Bye!");
	}

	void IDisposable.Dispose()
	{
#if OPEN_CV
		capture.Dispose();
#endif
		client.Dispose();
	}
}

public struct DonkeyCarMessage
{
	/// <summary>
	/// The angle of the wheels. From -1.0 to 1.0 for full left and full right respectively.
	/// </summary>
	[JsonPropertyName("angle")]
	public double Angle { get; set; }
	/// <summary>
	/// The throttle of the car. From -1.0 to 1.0 for full reverse and full forward respectively.
	/// </summary>
	[JsonPropertyName("throttle")]
	public double Throttle { get; set; }
	/// <summary>
	/// API controll or fully autonomous.
	/// </summary>
	/// <remarks>
	/// The internal "AI" ins't configured, use <see cref="DriveMode.User"/>.
	/// </remarks>
	[JsonPropertyName("drive_mode")]
	public DriveMode DriveMode { get; set; }
	/// <summary>
	/// Should this message, and related telemetry, be recorded and stored on the car?
	/// </summary>
	/// <remarks>
	/// No you shouldn't! Don't use this.
	/// </remarks>
	[JsonPropertyName("recording")]
	public bool Recording { get; set; }

	/// <summary>
	/// Instantiates a DonkeyCarMessage with the default <see cref="DriveMode"/> and <see cref="Recording"/> state.
	/// </summary>
	public DonkeyCarMessage(double angle, double throttle) :
		this(angle, throttle, DriveMode.User, false)
	{ }

	/// <summary>
	/// Instantiates a DonkeyCarMessage with full control.
	/// </summary>
	/// <remarks>
	/// You shouldn't need this.
	/// </remarks>
	public DonkeyCarMessage(double angle, double throttle, DriveMode driveMode, bool recording) =>
		(Angle, Throttle, DriveMode, Recording) =
			(angle, throttle, driveMode, recording);
}

public enum DriveMode
{
	/// <summary>
	/// The car is controlled by the API.
	/// </summary>
	User,
	/// <summary>
	/// The car is controlled by the internal "AI".
	/// </summary>
	Pilot,
}
