using System;
using Draft;
using System.Threading.Tasks;
using Draft.Responses;
using Newtonsoft.Json;
using System.Dynamic;
using Newtonsoft.Json.Converters;

namespace EtcdObjectSync
{
    public class SyncedValue<T> {
        [JsonProperty(PropertyName = "value")]
        public T Value { get; set; }
    }

    public class EtcdObjectSync
    {
        private IEtcdClient etcd;

        private T processKeyResult<T>(string rawValue)
        {
            if (typeof(T) == typeof(ExpandoObject))
            {
                var converter = new ExpandoObjectConverter();
                dynamic parsedExpando = JsonConvert.DeserializeObject<ExpandoObject>(rawValue, converter);
                return parsedExpando.value;
            }

            var parsedValue = JsonConvert.DeserializeObject<SyncedValue<T>>(rawValue);
            return parsedValue.Value;
        }

        public EtcdObjectSync(string etcdUrl = "http://localhost:4001")
        {
            etcd = Etcd.ClientFor(new Uri(etcdUrl));
        }

        public async Task Sync<T> (string key, Action<T> callback)
        {
            etcd.Watch(key)
            .Subscribe(syncResult =>
            {
                callback(processKeyResult<T>(syncResult.Data.RawValue));
            });

            try
            { 
                var keyResult = await etcd.GetKey(key);
                callback(processKeyResult<T>(keyResult.Data.RawValue));
            }
            catch (Exception ex)
            {
                callback(default(T));
            }
        }

        public async Task Sync (string key, Action<ExpandoObject> callback)
        {
            await Sync<ExpandoObject>(key, callback);
        }

        public async Task Set<T>(string key, T value)
        {
            var syncValue = new SyncedValue<T>() { Value = value };

            if (value == null)
            {
                await etcd.DeleteKey(key);
                return;
            }

            string valueAsString = JsonConvert.SerializeObject(syncValue);
            var keyResult = await etcd
                .UpsertKey(key)
                .WithValue(valueAsString);
        }
    }
}
