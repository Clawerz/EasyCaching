﻿namespace EasyCaching.UnitTests
{    
    using EasyCaching.Core;
    using EasyCaching.Core.Internal;
    using EasyCaching.HybridCache;
    using EasyCaching.InMemory;
    using EasyCaching.Redis;
    using FakeItEasy;
    using Microsoft.Extensions.Caching.Memory;
    using System;
    using System.Threading.Tasks;
    using Xunit;
    using Microsoft.Extensions.Options;

    public class HybridCachingTest : BaseCachingProviderTest
    {        
        public HybridCachingTest()
        {                        
            RedisCacheOptions options = new RedisCacheOptions()
            {
                //Password = ""
            };

            options.Endpoints.Add(new ServerEndPoint("127.0.0.1", 6379));

            var fakeOption = A.Fake<IOptions<RedisCacheOptions>>();

            A.CallTo(() => fakeOption.Value).Returns(options);

            var fakeDbProvider = A.Fake<RedisDatabaseProvider>(option => option.WithArgumentsForConstructor(new object[] { fakeOption }));

            var serializer = new DefaultBinaryFormatterSerializer();


            var serviceAccessor = A.Fake<Func<string, IEasyCachingProvider>>();

            A.CallTo(() => serviceAccessor(HybridCachingKeyType.LocalKey)).Returns(new InMemoryCachingProvider(new MemoryCache(new MemoryCacheOptions())));
            A.CallTo(() => serviceAccessor(HybridCachingKeyType.DistributedKey)).Returns(new DefaultRedisCachingProvider(fakeDbProvider,serializer));

            _provider = new HybridCachingProvider(serviceAccessor);
            _defaultTs = TimeSpan.FromSeconds(50);
        }

        [Fact]
        public void Set_Value_And_Get_Cached_Value_Should_Succeed()
        {
            var cacheKey = Guid.NewGuid().ToString();
            var cacheValue = "value";
            _provider.Set(cacheKey, cacheValue, _defaultTs);

            var val = _provider.Get<string>(cacheKey, null, _defaultTs);
            Assert.NotNull(val);
            Assert.Equal(cacheValue, val.Value);
        }

        [Fact]
        public async Task Set_Value_And_Get_Cached_Value_Async_Should_Succeed()
        {
            var cacheKey = Guid.NewGuid().ToString();
            var cacheValue = "value";
            await _provider.SetAsync(cacheKey, cacheValue, _defaultTs);

            var val = await _provider.GetAsync<string>(cacheKey, null, _defaultTs);
            Assert.NotNull(val);
            Assert.Equal(cacheValue, val.Value);
        }

        [Fact]
        public void Get_Not_Cached_Value_Should_Call_Retriever_And_Return_Value()
        {
            var cacheKey = Guid.NewGuid().ToString();
            var func = Create_Fake_Retriever_Return_String();

            var res = _provider.Get(cacheKey, func, _defaultTs);

            Assert.Equal("123", res.Value);
        }

        [Fact]
        public async Task Get_Not_Cached_Value_Async_Should_Call_Retriever_And_Return_Value()
        {
            var cacheKey = Guid.NewGuid().ToString();
            var func = Create_Fake_Retriever_Return_String_Async();

            var res = await _provider.GetAsync(cacheKey, func, _defaultTs);

            Assert.Equal("123", res.Value);
        }

        [Fact]
        public void Get_Not_Cached_Value_Without_Retriever_Should_Return_Default_Value()
        {
            var cacheKey = Guid.NewGuid().ToString();

            var res = _provider.Get<string>(cacheKey);

            Assert.Equal(default(string), res.Value);
        }

        [Fact]
        public async Task Get_Not_Cached_Value_Without_Retriever_Async_Should_Return_Default_Value()
        {
            var cacheKey = Guid.NewGuid().ToString();

            var res = await _provider.GetAsync<string>(cacheKey);

            Assert.Equal(default(string), res.Value);
        }

        [Fact]
        public void Get_Cached_Value_Without_Retriever_Should_Return_Default_Value()
        {
            var cacheKey = Guid.NewGuid().ToString();

            _provider.Set(cacheKey,"123",_defaultTs);

            var res = _provider.Get<string>(cacheKey);

            Assert.Equal("123", res.Value);
        }

        [Fact]
        public async Task Get_Cached_Value_Without_Retriever_Async_Should_Return_Default_Value()
        {
            var cacheKey = Guid.NewGuid().ToString();

            await _provider.SetAsync(cacheKey, "123", _defaultTs);

            var res = await _provider.GetAsync<string>(cacheKey);

            Assert.Equal("123", res.Value);
        }

        [Fact]
        public void Remove_Cached_Value_Should_Succeed()
        {
            var cacheKey = Guid.NewGuid().ToString();
            var cacheValue = "value";
            _provider.Set(cacheKey, cacheValue, _defaultTs);
            var valBeforeRemove = _provider.Get<string>(cacheKey, null, _defaultTs);
            Assert.NotNull(valBeforeRemove);

            _provider.Remove(cacheKey);
            var valAfterRemove = _provider.Get(cacheKey, () => "123", _defaultTs);
            Assert.Equal("123", valAfterRemove.Value);
        }      

        [Fact]
        public async Task Remove_Cached_Value_Async_Should_Succeed()
        {
            var cacheKey = Guid.NewGuid().ToString();
            var cacheValue = "value";
            await _provider.SetAsync(cacheKey, cacheValue, _defaultTs);
            var valBeforeRemove = await _provider.GetAsync<string>(cacheKey, null, _defaultTs);
            Assert.NotNull(valBeforeRemove);

            await _provider.RemoveAsync(cacheKey);
            var valAfterRemove = await _provider.GetAsync(cacheKey,async () => await Task.FromResult("123"), _defaultTs);
            Assert.Equal("123", valAfterRemove.Value);
        }  

        [Fact]
        public void Refresh_Should_Succeed()
        {
            var cacheKey = Guid.NewGuid().ToString();
            var cacheValue = "value";
            _provider.Set(cacheKey, cacheValue, _defaultTs);

            var tmp = _provider.Get<string>(cacheKey);
            Assert.Equal("value", tmp.Value);

            _provider.Refresh(cacheKey, "NewValue", _defaultTs);

            var act = _provider.Get<string>(cacheKey);

            Assert.Equal("NewValue", act.Value);
        }

        [Fact]
        public async Task Refresh_Async_Should_Succeed()
        {
            var cacheKey = Guid.NewGuid().ToString();
            var cacheValue = "value";
            await _provider.SetAsync(cacheKey, cacheValue, _defaultTs);

            var tmp = await _provider.GetAsync<string>(cacheKey);
            Assert.Equal("value", tmp.Value);

            await _provider.RefreshAsync(cacheKey, "NewValue", _defaultTs);

            var act = await _provider.GetAsync<string>(cacheKey);

            Assert.Equal("NewValue", act.Value);
        }

        [Fact]
        public void RemoveByPrefix_Should_Succeed()
        {
            SetCacheItem("demo:1", "1");
            SetCacheItem("demo:2", "2");
            SetCacheItem("demo:3", "3");
            SetCacheItem("demo:4", "4");
            SetCacheItem("xxx:1", "xxx");

            _provider.RemoveByPrefix("demo");

            var demo1 = _provider.Get<string>("demo:1");
            var demo2 = _provider.Get<string>("demo:2");
            var demo3 = _provider.Get<string>("demo:3");
            var demo4 = _provider.Get<string>("demo:4");
            var xxx1 = _provider.Get<string>("xxx:1");

            Assert.False(demo1.HasValue);
            Assert.False(demo2.HasValue);
            Assert.False(demo3.HasValue);
            Assert.False(demo4.HasValue);
            Assert.True(xxx1.HasValue);
        }

        [Fact]
        public async Task RemoveByPrefixAsync_Should_Succeed()
        {
            SetCacheItem("demo:1", "1");
            SetCacheItem("demo:2", "2");
            SetCacheItem("demo:3", "3");
            SetCacheItem("demo:4", "4");
            SetCacheItem("xxx:1", "xxx");

            await _provider.RemoveByPrefixAsync("demo");

            var demo1 = _provider.Get<string>("demo:1");
            var demo2 = _provider.Get<string>("demo:2");
            var demo3 = _provider.Get<string>("demo:3");
            var demo4 = _provider.Get<string>("demo:4");
            var xxx1 = _provider.Get<string>("xxx:1");

            Assert.False(demo1.HasValue);
            Assert.False(demo2.HasValue);
            Assert.False(demo3.HasValue);
            Assert.False(demo4.HasValue);
            Assert.True(xxx1.HasValue);
        }

        private void SetCacheItem(string cacheKey, string cacheValue)
        {
            _provider.Set(cacheKey, cacheValue, _defaultTs);

            var val = _provider.Get<string>(cacheKey);
            Assert.Equal(cacheValue, val.Value);
        }
    }
}
