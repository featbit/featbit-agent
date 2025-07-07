## Troubleshooting

### Connection Issues

**Connection refused to FeatBit server:**

- Verify network connectivity between the FeatBit Agent and the Evaluation Server (ELS)
- Ensure the ELS is running and accessible from the agent's network
- Check if the `StreamingUri` and `ApiKey` are correctly configured
- Verify firewall rules allow outbound connections on the required ports

**WebSocket connection failures:**

- Confirm the `StreamingUri` uses the correct protocol (`ws://` or `wss://`)
- Check for proxy servers or network middleware that might block WebSocket connections
- Ensure the target server supports WebSocket upgrades

### Health Check Issues

**Readiness check failures:**

- Test the readiness endpoint manually: `curl http://your-agent-host/health/readiness`
- In **auto mode**: Verify the agent can connect to the ELS and sync initial data
- In **manual mode**: Ensure bootstrap configuration files are properly loaded
- Check application logs for detailed error messages: `docker logs featbit-agent`

**Liveness check failures:**

- Verify the agent process is running: `docker ps` or `ps aux | grep featbit-agent`
- Check if port 6100 is accessible and not blocked by firewall rules
- Review system resources (CPU, memory) to ensure the agent has sufficient resources