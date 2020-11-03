using System;
using System.Threading.Tasks;

namespace CheckWebsiteStatus.SimpleLogger
{

    public interface ICLogger
    {
        Task Log(string message);

    }

    public class IccLogger<T> : ICLogger
    {
        public async Task Log(string message)
        {
            var lineToPrint = $"{DateTime.Now} :: {typeof(T).Name} : {message}";
            //Console.WriteLine(lineToPrint);
            await Console.Out.WriteLineAsync(lineToPrint);
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