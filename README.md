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