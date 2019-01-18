namespace ExchangeBroker.Services
{
    public interface ICryptoProviderService : ICryptoService
    {
        ICryptoService GetService(string currency);
    }
}