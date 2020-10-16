using System;

namespace CheckWebsiteStatus.SimpleLogger
{

    public interface ICLogger
    {
        void Log(string message);

    }

    public class IccLogger<T> : ICLogger
    {
        public void Log(string message)
        {
            var lineToPrint = $"{DateTime.Now} :: {typeof(T).Name} : {message}";
            Console.WriteLine(lineToPrint);
        }
    }
    
    
    public static class CLogger<T> where T: class
    {
        public static ICLogger GetLogger()
        {
            return new IccLogger<T>();
        }
    }
}