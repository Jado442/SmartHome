namespace SmartHome.Services
{
    public interface ILightService
    {
        bool IsLightOn { get; }
        void TurnOn();
        void TurnOff();
    }
}
