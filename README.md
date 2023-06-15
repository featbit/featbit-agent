# FeatBit Agent

The FeatBit Agent is a lightweight .NET application designed to **run within your infrastructure**. It handles all
connections to the FeatBit platform, fetches all of your flag and segment data on startup, caches it, and delivering
updates to connected downstream SDKs.

Rather than every client/server connecting directly to the FeatBit platform and streaming changes, they will connect
directly to your installed agent to receive any flag updates. The agent can support connecting to multiple environments
across different projects for your account, making it a 1:1 replacement for a direct connection.

## Use Cases

You may consider setting up the FeatBit Agent in the following scenarios:

- **Air-gapped environments**: You may be required to operate in environments without internet connectivity. In this
  situation, running the FeatBit Agent in offline mode provides the capabilities of FeatBit without using external
  services.
- **Reducing outbound connections**: With the default configuration, each SDK instance you run will reach out to
  the FeatBit to fetch all the initial flag data, then maintain an outbound streaming connection to receive any
  updates you make to that environment. Use the FeatBit agent so your servers can connect directly to hosts
  in your own data center, instead of connecting directly to FeatBit's streaming API.
- **Security and Privacy**: In environments with strict security protocols, your application might be restricted from
  establishing third-party connections. By deploying the FeatBit Agent within your customers' own environments, you can
  overcome this limitation. Since the agent operates locally, all user information will remain within your customers'
  environments.

## Installation

### Download

To begin, you'll need to download the FeatBit Agent from
the [GitHub releases](https://github.com/featbit/featbit-agent/releases) page to your hosting server.

```bash
# !! Replace the URL with the desired version, here we choose linux-x64 for example 
wget https://github.com/featbit/featbit-agent/releases/download/v1.0.0/featbit_agent_linux-x64_1.0.0.tar.gz
tar -xvzf featbit_agent_linux-x64_1.0.0.tar.gz --one-top-level=featbit-agent
cd featbit-agent
```

Once the download is complete, perform a quick test to verify that Featbit Agent can run on your machine.

```bash
# Run agent from command line
# If the Api file is not executable, Use 'chmod +x Api' to allow execution of the executable file
./Api
```

If everything is fine, you should see the following output:

```log
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
info: Microsoft.Hosting.Lifetime[0]
      Hosting environment: Production
info: Microsoft.Hosting.Lifetime[0]
      Content root path: /home/ubuntu/featbit-agent/
```

### Run As A Service (Using Systemd on Linux)

We need to set up a process manager that starts the agent when requests arrive and restarts the agent after it
crashes or the server reboots. Here we use [systemd](https://systemd.io/) for example.

#### Create the service file

Create the service definition file:

```bash
sudo vi /etc/systemd/system/featbit-agent.service
```

Then copy-paste below content into it, **replacing the necessary configurations** and save the file.

```ini
[Unit]
Description = The FeatBit Agent Service

[Service]
# !! replace with your own featbit agent extraction location path here, for example: /home/ubuntu/
ExecStart=/your/location/to/featbit-agent/Api
WorkingDirectory=/your/location/to/featbit-agent

# !! replace with your user name, for example: ubuntu
User=your-user-name

# !! replace with your agent key, for example: rp-MzM3OTE5MTk0**************
Environment=ApiKey=your-api-key

# Restart service after 10 seconds if the featbit agent crashes:
Restart=always
RestartSec=10

KillSignal=SIGINT

# to query logs using journalctl, set a logical name here
SyslogIdentifier=featbit-agent

[Install]
WantedBy=multi-user.target
```

Now, let's start the service and check its status:

```bash
# Start the FeatBit Agent service and check its status
sudo systemctl start featbit-agent.service
sudo systemctl status featbit-agent.service
```

If everything is fine, you should see the following output:

```log
● featbit-agent.service - The FeatBit Agent Service
     Loaded: loaded (/etc/systemd/system/featbit-agent.service; disabled; vendor preset: enabled)
     Active: active (running) since Thu 2023-06-15 03:35:26 UTC; 1h 40min ago
   Main PID: 2321 (Api)
      Tasks: 14 (limit: 1141)
     Memory: 31.9M
        CPU: 2.814s
     CGroup: /system.slice/featbit-agent.service
             └─2321 /home/ubuntu/featbit-agent/Api

Jun 15 03:35:26 ip-172-31-37-23 systemd[1]: Started The FeatBit Agent Service.
Jun 15 03:35:28 ip-172-31-37-23 featbit-agent[2321]: info: Microsoft.Hosting.Lifetime[14]
Jun 15 03:35:28 ip-172-31-37-23 featbit-agent[2321]:       Now listening on: http://localhost:5000
Jun 15 03:35:28 ip-172-31-37-23 featbit-agent[2321]: info: Microsoft.Hosting.Lifetime[0]
Jun 15 03:35:28 ip-172-31-37-23 featbit-agent[2321]:       Application started. Press Ctrl+C to shut down.
Jun 15 03:35:28 ip-172-31-37-23 featbit-agent[2321]: info: Microsoft.Hosting.Lifetime[0]
Jun 15 03:35:28 ip-172-31-37-23 featbit-agent[2321]:       Hosting environment: Production
Jun 15 03:35:28 ip-172-31-37-23 featbit-agent[2321]: info: Microsoft.Hosting.Lifetime[0]
Jun 15 03:35:28 ip-172-31-37-23 featbit-agent[2321]:       Content root path: /home/ubuntu/featbit-agent/
```

To enable automatic startup of the agent when the OS starts, run the following command:

```bash
sudo systemctl enable featbit-agent.service
```

### View logs

Since the FeatBit Agent is managed by systemd, all events and processes are logged to a centralized
journal. To view the `featbit-agent.service`-specific logs, use the following command:

```bash
sudo journalctl -fu featbit-agent.service
```

For further filtering, time options such as `--since today`, `--until 1 hour ago`, or a combination of these can reduce the
number of entries returned.

```bash
sudo journalctl -fu featbit-agent.service --since "2023-06-15" --until "2023-06-15 12:00" 
```

### Health Check
You can run the following command to check if the agent is healthy:

```bash
curl http://localhost:5000/health/liveness; echo
```