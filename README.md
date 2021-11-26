# Chalmers Cup in Self-driving

Please change the branch to use one of our provided templates.

## Python

For the Python implementation, you need to install the following packages:
* websocket
* cv2

It is recommended to use a virtual environment, using conda.

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
