# FeatBit Agent

The FeatBit Agent is a lightweight .NET application designed to **run within your infrastructure**. It handles all
connections to the FeatBit platform, fetches all of your flag and segment data on startup, caches it, and delivering
updates to connected downstream SDKs.

Rather than every client/server connecting directly to the FeatBit platform and streaming changes, they will connect
directly to your installed agent to receive any flag updates. The agent can support connecting to multiple environments
across different projects for your organization, making it a 1:1 replacement for a direct connection.

## Use Cases

You may consider setting up the FeatBit Agent in the following scenarios:

- **Air-gapped environments**: You may be required to operate in environments without internet connectivity. In this
  situation, running the FeatBit Agent in `manual` mode provides the capabilities of FeatBit without using external
  services.
- **Reducing outbound connections**: With the default configuration, each SDK instance you run will reach out to
  the FeatBit to fetch all the initial flag data, then maintain an outbound streaming connection to receive any
  updates you make to that environment. Use the FeatBit agent so your servers can connect directly to hosts
  in your own data center, instead of connecting directly to FeatBit's streaming API.
- **Security and Privacy**: In environments with strict security protocols, your application might be restricted from
  establishing third-party connections. By deploying the FeatBit Agent within your customers' own environments, you can
  overcome this limitation. Since the agent operates locally, all user information will remain within your customers'
  environments.

## Get Started

> **Note**
> Before getting started, you should have a good understanding of what
> a [relay proxy](https://docs.featbit.co/relay-proxy/relay-proxy) is.

The easiest way to get started with the FeatBit Agent is using Docker:

#### Use Docker Compose

1. **Clone or download the repository:**
   ```bash
   git clone https://github.com/featbit/featbit-agent.git
   cd featbit-agent
   ```

2. **Configure the agent:**
   Edit the environment variables in `docker-compose.yml`:
   ```yaml
   environment:
     - AgentId=your-unique-agent-id
     - StreamingUri=ws://your-els-server
     - ApiKey=your-api-key
     - EventUri=http://your-event-server
   ```

3. **Start the agent:**
   ```bash
   docker-compose up -d
   ```

4. **Verify it's running:**
   ```bash
   curl http://localhost:6100/health/liveness
   ```

#### Using Docker directly

```bash
# Run the container
docker run -d \
  --name featbit-agent \
  -p 6100:6100 \
  -e Mode=auto \
  -e AgentId=docker-agent-001 \
  -e StreamingUri=ws://your-els-server \
  -e ApiKey=your-api-key \
  -e EventUri=http://your-event-server \
  featbit/featbit-agent
```

## Environment Variables

| Variable        | Description                                                                                          | Default         |
|-----------------|------------------------------------------------------------------------------------------------------|-----------------|
| Mode            | Operation mode of the agent (`auto` or `manual`)                                                     | `auto`          |
| AgentId         | Unique identifier for the agent, required in `auto` mode for agent auto registration                 | ''              |
| StreamingUri    | Evaluation server streaming uri, for example: `ws://your-els-server`                                 | ''              |
| ApiKey          | API Key of the relay proxy                                                                           | ''              |
| EventUri        | Event server uri, usually the same as evaluation server uri, for example: `http://your-event-server` | ''              |
| ForwardEvents   | Whether forward insights data (flag evaluation event, end users, etc) to the FeatBit server          | `true`          |
| ASPNETCORE_URLS | URLs the agent listens on                                                                            | `http://+:6100` |

## Health Checks

you have a few options to check the app's health status

### Liveness

Run `curl your-agent-host/health/liveness` to verify that the agent has started and is running.
This only exercises the most basic requirements of the agent itself i.e. can they respond to an HTTP request.

### Readiness

Run `curl your-agent-host/health/readiness` to verify if the agent is working correctly and ready to serve streaming
requests.

- for `auto` mode, it checks if the agent's data synchronizer is stable and has fetched the initial data.
- for `manual` mode, it checks if the agent has been bootstrapped manually.

## Troubleshooting

### Common Issues

1. **Connection refused to FeatBit server:**
    - Verify network connectivity between the FeatBit Agent and the ELS
    - Ensure the ELS is running and accessible
    - Check if the `StreamingUri` and `ApiKey` are correctly set

2. **Health check failures:**
    - Test readiness endpoints manually: `curl {your-agent-host}/health/readiness`
    - Check application logs: `docker logs featbit-agent`