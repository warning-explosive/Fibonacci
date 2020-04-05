namespace RabbitBasics
{
    public class RabbitConnectionConfig
    {
        public string Host { get; set; }
            
        public string VirtualHost { get; set; }
            
        public string Username { get; set; }
            
        public string Password { get; set; }
            
        public string Product { get; set; }
            
        public ushort SecondsTimeout { get; set; }

        public override string ToString()
        {
            return $"host={Host};virtualHost={VirtualHost};username={Username};password={Password};product={Product};timeout={SecondsTimeout}";
        }
    }
}