namespace SmartHome.Services
{
    public class LightService : ILightService

    {
        public bool IsLightOn { get; private set; }
        public void TurnOn() => IsLightOn = true;
        public void TurnOff() => IsLightOn = false;
    }
}
