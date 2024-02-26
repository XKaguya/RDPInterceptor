# RDPInterceptor
RDPInterceptor is a tool that helps personal server owner to only allowed specific IP address can connect.

# Usage
## Cmd
### `.\RDPInterceptor.exe --WebOnly`

## GUI
### Just double-click the exe.

# Features
## WebUI Management
### You can remotely manage the Whitelist by connect to the port 5000(HTTP)/5001(HTTPS). HTTPS perfer.
### All the things you can do on the program also can do remotely.
### You can also only start the WebService.

### WebUI is protected by cookie. The default username and password is `admin/admin`.You can change this in the login page.
### Cookie's life cycle is 10m.
### Username and Password are stored at `auth.xml`. The password is encrypted with SHA-256.

## IP Whitelist
### Only specific ip address can make connection with host.

# TODO
## Custom RDP Port.
## More ways to interceptor.
## And many many maybe.
