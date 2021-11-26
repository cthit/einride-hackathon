# Chalmers Cup in Self-driving

Please change the branch to use one of our provided templates.

## C#

This is a normal .NET Core C# project. To run the program, simply open your
terminal and enter `dotnet run`. It can also be run from Visual Studio by
opening the `donkeycar.csproj` file.

The project contains a `DonkeyCarClient` class that is setup to communicate with
the DonkeyCar. Create an instance by providing host name (or IP address) and
port, then use the `Send` method to send commands to the car, and the
`FetchFrame` method to collect the latest frame from the cars camera.

Take a look at the `Main` function to see the client and it's methods in action.

### Emgu.CV

Watch out for common pitfalls when using Emgu.CV on Linux of MaxOS. Look through
the [Download And Installation](https://www.emgu.com/wiki/index.php/Download_And_Installation)
wiki page for instructions and remember to update the nuget dependency in
`donkeycar.csproj`.

Emgu.CV can also easily be removed by removing the specified line in
`donkeycar.csproj`. When programming without Emgu.CV the static method
`ConstructVideoUrl` can be useful to, as the name suggests, construct a string
containing the URL to the video feed.

## Connect to your car

Setup up a WiFi with the following settings:

SSID: `<animal>` (lowercase)
Password: `Donkeycar2021`

You can do this on your your phone, or similar.

The car will connect to this network and start the server immediately after
being turned on.

## How to control your car

Connect to the same WiFi LAN as your car and you will be able to control it
remotely. To test this, navigate to `http://<animal>:8887/drive`,
here you will see a camera feed from the car and controls for steering/throttle.
Your program will use the same underlying interface to programmatically drive
the car.

### The camera feed

You can find the camera feed at `http://<animal>:8887/video`. Do with
it whatever you like. If you don't have any other preferences, we suggest you
try using [OpenCV](https://opencv.org/) to fetch and analyze the feed.

### The steering and throttle

To steer and throttle the car you send a simple message to it over WebSocket.
The address here is `ws://<animal>:8887/wsDrive`.

The message you send is in JSON format and should look like this:

```json
{
	"angle": 0.0,
	"throttle": 0.2,
	"drive_mode": "user",
	"recording": false
}
```

The `angle` property goes from `-1.0`, for full left, and `1.0`, for full right.
Likewise, the `throttle` goes from `-1.0`, for full reverse, and `1.0`, for full
forward.

Don't mind, or change, the other properties.
