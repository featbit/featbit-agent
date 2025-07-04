using Api.Shared;

namespace Api.Services;

public interface IStatusProvider
{
    Status GetStatus();
}