# RDPInterceptor
RDPInterceptor is a tool that helps server owners to only allow specific IP addresses to connect.
| GUI Mode | WebUI |
|-------|-------|
| ![GUI ModeGUI Mode](https://github.com/XKaguya/RDPInterceptor/blob/main/Pic/GUI%20Mode.png) | ![WebUI](https://github.com/XKaguya/RDPInterceptor/blob/main/Pic/WebUI.png)|

# Usage

## Cmd

### `.\RDPInterceptor.exe --WebOnly`

## GUI

### Just double-click the exe.

# Features

## `WebUI Management`

You can remotely manage the whitelist by connecting to port 5000.

All the tasks you can perform using the program can also be done remotely.

You can also start only the web service.

The WebUI is protected by a cookie. The default username and password are `admin/admin`. You can change this on the login page.

The cookie's lifespan is 10 minutes.

Usernames and passwords are stored in `auth.xml`. Passwords are encrypted using the SHA-256 algorithm.

## `IP Whitelist`

Only specific IP addresses can make connections with the host.

# TODO

More interception methods.

And many more possibilities.
