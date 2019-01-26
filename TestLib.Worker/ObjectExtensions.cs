using Newtonsoft.Json;
using System.Net.Http;
using System.Text;

namespace TestLib.Worker
{
	internal static class ObjectExtensions
	{
		internal static string SerializeObjectToJson(object o, bool skipNull = false)
		{
			var settings = new JsonSerializerSettings();

			if (skipNull)
			{
				settings.NullValueHandling = NullValueHandling.Ignore;
			}

			return JsonConvert.SerializeObject(o, Formatting.None, settings);
		}
		public static StringContent AsJson(this object o, bool skipNull = false)
			=> new StringContent(SerializeObjectToJson(o, skipNull), Encoding.UTF8, "application/json");
	}
}