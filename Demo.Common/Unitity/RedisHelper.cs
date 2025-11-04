using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;

namespace Demo.Common.Unitity
{
    public class RedisHelper
    {
        public static ConnectionMultiplexer _instance = ConnectionMultiplexer.Connect("localhost");


        public static void Test()
        {
            var db = _instance.GetDatabase();
            db.KeyExpire("key1",TimeSpan.FromDays(1));

            var val = db.Multiplexer;
            
            var sub  = _instance.GetSubscriber();
            sub.Publish("11","Hello");
        }
    }
}
