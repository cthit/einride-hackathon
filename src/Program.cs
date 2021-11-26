using System;
using System.Threading.Tasks;
using Emgu.CV;

public class Program
{
	public static async Task Main()
	{
		// Instansiate the client with host and port
		using DonkeyCarClient client = new DonkeyCarClient("donkeycar");

		// Listening to events. Can be removed if the get too "chatty".
		client.OnMessageSent += static msg => Console.WriteLine($"Sent:\t\t{msg}");
		client.OnMessageReceived += static msg => Console.WriteLine($"Received:\t\t{msg}");
		client.OnDisconnect += static reason => Console.WriteLine($"Disconnected:\t\t{reason}");
		client.OnReconnect += static reason => Console.WriteLine($"Reconnected:\t\t{reason}");

		// Connect to the server
		await client.Connect();

		// Instantiate an empty frame reference
		Mat frame = new Mat();
		while (client.IsConnected)
		{
			// Populate the frame with data from the latest camera snapshot
			await client.FetchFrame(frame);

			//Work some magic based on the frame
			double angle = 0.00d;
			double throttle = 0.20d;

			// Send the angle and throttle to the car
			// The car will continue based on these values until the next
			// update is received
			await client.Send(angle, throttle);
		}
	}
}
