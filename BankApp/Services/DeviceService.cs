using System;
using System.IO;

namespace BankApp.Services
{
    public static class DeviceService
    {
        private static string deviceFile =
            "device.id";

        public static string GetDeviceId()
        {
            if (File.Exists(deviceFile))
            {
                return File.ReadAllText(deviceFile);
            }

            string id = Guid.NewGuid().ToString();

            File.WriteAllText(deviceFile, id);

            return id;
        }

        public static string GetDeviceName()
        {
            return Environment.MachineName;
        }
    }
}