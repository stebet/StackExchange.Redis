﻿using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace StackExchange.Redis.Tests
{
    public class Transactions : TestBase
    {
        public Transactions(ITestOutputHelper output) : base (output) { }

        [Fact]
        public void BasicEmptyTran()
        {
            using (var muxer = Create())
            {
                RedisKey key = Me();
                var db = muxer.GetDatabase();
                db.KeyDelete(key, CommandFlags.FireAndForget);
                Assert.False(db.KeyExists(key));

                var tran = db.CreateTransaction();

                var result = tran.Execute();
                Assert.True(result);
            }
        }

        [Theory]
        [InlineData(false, false, true)]
        [InlineData(false, true, false)]
        [InlineData(true, false, false)]
        [InlineData(true, true, true)]
        public void BasicTranWithExistsCondition(bool demandKeyExists, bool keyExists, bool expectTranResult)
        {
            using (var muxer = Create(disabledCommands: new[] { "info", "config" }))
            {
                RedisKey key = Me(), key2 = Me() + "2";
                var db = muxer.GetDatabase();
                db.KeyDelete(key, CommandFlags.FireAndForget);
                db.KeyDelete(key2, CommandFlags.FireAndForget);
                if (keyExists) db.StringSet(key2, "any value", flags: CommandFlags.FireAndForget);
                Assert.False(db.KeyExists(key));
                Assert.Equal(keyExists, db.KeyExists(key2));

                var tran = db.CreateTransaction();
                var cond = tran.AddCondition(demandKeyExists ? Condition.KeyExists(key2) : Condition.KeyNotExists(key2));
                var incr = tran.StringIncrementAsync(key);
                var exec = tran.ExecuteAsync();
                var get = db.StringGet(key);

                Assert.Equal(expectTranResult, db.Wait(exec));
                if (demandKeyExists == keyExists)
                {
                    Assert.True(db.Wait(exec), "eq: exec");
                    Assert.True(cond.WasSatisfied, "eq: was satisfied");
                    Assert.Equal(1, db.Wait(incr)); // eq: incr                    
                    Assert.Equal(1, (long)get); // eq: get
                }
                else
                {
                    Assert.False(db.Wait(exec), "neq: exec");
                    Assert.False(cond.WasSatisfied, "neq: was satisfied");
                    Assert.Equal(TaskStatus.Canceled, incr.Status); // neq: incr                    
                    Assert.Equal(0, (long)get); // neq: get
                }
            }
        }

        [Theory]
        [InlineData("same", "same", true, true)]
        [InlineData("x", "y", true, false)]
        [InlineData("x", null, true, false)]
        [InlineData(null, "y", true, false)]
        [InlineData(null, null, true, true)]

        [InlineData("same", "same", false, false)]
        [InlineData("x", "y", false, true)]
        [InlineData("x", null, false, true)]
        [InlineData(null, "y", false, true)]
        [InlineData(null, null, false, false)]
        public void BasicTranWithEqualsCondition(string expected, string value, bool expectEqual, bool expectTranResult)
        {
            using (var muxer = Create())
            {
                RedisKey key = Me(), key2 = Me() + "2";
                var db = muxer.GetDatabase();
                db.KeyDelete(key, CommandFlags.FireAndForget);
                db.KeyDelete(key2, CommandFlags.FireAndForget);

                if (value != null) db.StringSet(key2, value, flags: CommandFlags.FireAndForget);
                Assert.False(db.KeyExists(key));
                Assert.Equal(value, (string)db.StringGet(key2));

                var tran = db.CreateTransaction();
                var cond = tran.AddCondition(expectEqual ? Condition.StringEqual(key2, expected) : Condition.StringNotEqual(key2, expected));
                var incr = tran.StringIncrementAsync(key);
                var exec = tran.ExecuteAsync();
                var get = db.StringGet(key);

                Assert.Equal(expectTranResult, db.Wait(exec));
                if (expectEqual == (value == expected))
                {
                    Assert.True(db.Wait(exec), "eq: exec");
                    Assert.True(cond.WasSatisfied, "eq: was satisfied");
                    Assert.Equal(1, db.Wait(incr)); // eq: incr
                    Assert.Equal(1, (long)get); // eq: get
                }
                else
                {
                    Assert.False(db.Wait(exec), "neq: exec");
                    Assert.False(cond.WasSatisfied, "neq: was satisfied");
                    Assert.Equal(TaskStatus.Canceled, incr.Status); // neq: incr
                    Assert.Equal(0, (long)get); // neq: get
                }
            }
        }

        [Theory]
        [InlineData(false, false, true)]
        [InlineData(false, true, false)]
        [InlineData(true, false, false)]
        [InlineData(true, true, true)]
        public void BasicTranWithHashExistsCondition(bool demandKeyExists, bool keyExists, bool expectTranResult)
        {
            using (var muxer = Create(disabledCommands: new[] { "info", "config" }))
            {
                RedisKey key = Me(), key2 = Me() + "2";
                var db = muxer.GetDatabase();
                db.KeyDelete(key, CommandFlags.FireAndForget);
                db.KeyDelete(key2, CommandFlags.FireAndForget);
                RedisValue hashField = "field";
                if (keyExists) db.HashSet(key2, hashField, "any value", flags: CommandFlags.FireAndForget);
                Assert.False(db.KeyExists(key));
                Assert.Equal(keyExists, db.HashExists(key2, hashField));

                var tran = db.CreateTransaction();
                var cond = tran.AddCondition(demandKeyExists ? Condition.HashExists(key2, hashField) : Condition.HashNotExists(key2, hashField));
                var incr = tran.StringIncrementAsync(key);
                var exec = tran.ExecuteAsync();
                var get = db.StringGet(key);

                Assert.Equal(expectTranResult, db.Wait(exec));
                if (demandKeyExists == keyExists)
                {
                    Assert.True(db.Wait(exec), "eq: exec");
                    Assert.True(cond.WasSatisfied, "eq: was satisfied");
                    Assert.Equal(1, db.Wait(incr)); // eq: incr
                    Assert.Equal(1, (long)get); // eq: get
                }
                else
                {
                    Assert.False(db.Wait(exec), "neq: exec");
                    Assert.False(cond.WasSatisfied, "neq: was satisfied");
                    Assert.Equal(TaskStatus.Canceled, incr.Status); // neq: incr
                    Assert.Equal(0, (long)get); // neq: get
                }
            }
        }

        [Theory]
        [InlineData("same", "same", true, true)]
        [InlineData("x", "y", true, false)]
        [InlineData("x", null, true, false)]
        [InlineData(null, "y", true, false)]
        [InlineData(null, null, true, true)]

        [InlineData("same", "same", false, false)]
        [InlineData("x", "y", false, true)]
        [InlineData("x", null, false, true)]
        [InlineData(null, "y", false, true)]
        [InlineData(null, null, false, false)]
        public void BasicTranWithHashEqualsCondition(string expected, string value, bool expectEqual, bool expectedTranResult)
        {
            using (var muxer = Create())
            {
                RedisKey key = Me(), key2 = Me() + "2";
                var db = muxer.GetDatabase();
                db.KeyDelete(key, CommandFlags.FireAndForget);
                db.KeyDelete(key2, CommandFlags.FireAndForget);

                RedisValue hashField = "field";
                if (value != null) db.HashSet(key2, hashField, value, flags: CommandFlags.FireAndForget);
                Assert.False(db.KeyExists(key));
                Assert.Equal(value, (string)db.HashGet(key2, hashField));

                var tran = db.CreateTransaction();
                var cond = tran.AddCondition(expectEqual ? Condition.HashEqual(key2, hashField, expected) : Condition.HashNotEqual(key2, hashField, expected));
                var incr = tran.StringIncrementAsync(key);
                var exec = tran.ExecuteAsync();
                var get = db.StringGet(key);

                Assert.Equal(expectedTranResult, db.Wait(exec));
                if (expectEqual == (value == expected))
                {
                    Assert.True(db.Wait(exec), "eq: exec");
                    Assert.True(cond.WasSatisfied, "eq: was satisfied");
                    Assert.Equal(1, db.Wait(incr)); // eq: incr
                    Assert.Equal(1, (long)get); // eq: get
                }
                else
                {
                    Assert.False(db.Wait(exec), "neq: exec");
                    Assert.False(cond.WasSatisfied, "neq: was satisfied");
                    Assert.Equal(TaskStatus.Canceled, incr.Status); // neq: incr
                    Assert.Equal(0, (long)get); // neq: get
                }
            }
        }

        [Theory]
        [InlineData(false, false, true)]
        [InlineData(false, true, false)]
        [InlineData(true, false, false)]
        [InlineData(true, true, true)]
        public void BasicTranWithListExistsCondition(bool demandKeyExists, bool keyExists, bool expectTranResult)
        {
            using (var muxer = Create(disabledCommands: new[] { "info", "config" }))
            {
                RedisKey key = Me(), key2 = Me() + "2";
                var db = muxer.GetDatabase();
                db.KeyDelete(key, CommandFlags.FireAndForget);
                db.KeyDelete(key2, CommandFlags.FireAndForget);
                if (keyExists) db.ListRightPush(key2, "any value", flags: CommandFlags.FireAndForget);
                Assert.False(db.KeyExists(key));
                Assert.Equal(keyExists, db.KeyExists(key2));

                var tran = db.CreateTransaction();
                var cond = tran.AddCondition(demandKeyExists ? Condition.ListIndexExists(key2, 0) : Condition.ListIndexNotExists(key2, 0));
                var push = tran.ListRightPushAsync(key, "any value");
                var exec = tran.ExecuteAsync();
                var get = db.ListGetByIndex(key, 0);

                Assert.Equal(expectTranResult, db.Wait(exec));
                if (demandKeyExists == keyExists)
                {
                    Assert.True(db.Wait(exec), "eq: exec");
                    Assert.True(cond.WasSatisfied, "eq: was satisfied");
                    Assert.Equal(1, db.Wait(push)); // eq: push
                    Assert.Equal("any value", (string)get); // eq: get
                }
                else
                {
                    Assert.False(db.Wait(exec), "neq: exec");
                    Assert.False(cond.WasSatisfied, "neq: was satisfied");
                    Assert.Equal(TaskStatus.Canceled, push.Status); // neq: push
                    Assert.Null((string)get); // neq: get
                }
            }
        }

        [Theory]
        [InlineData("same", "same", true, true)]
        [InlineData("x", "y", true, false)]
        [InlineData("x", null, true, false)]
        [InlineData(null, "y", true, false)]
        [InlineData(null, null, true, true)]

        [InlineData("same", "same", false, false)]
        [InlineData("x", "y", false, true)]
        [InlineData("x", null, false, true)]
        [InlineData(null, "y", false, true)]
        [InlineData(null, null, false, false)]
        public void BasicTranWithListEqualsCondition(string expected, string value, bool expectEqual, bool expectTranResult)
        {
            using (var muxer = Create())
            {
                RedisKey key = Me(), key2 = Me() + "2";
                var db = muxer.GetDatabase();
                db.KeyDelete(key, CommandFlags.FireAndForget);
                db.KeyDelete(key2, CommandFlags.FireAndForget);

                if (value != null) db.ListRightPush(key2, value, flags: CommandFlags.FireAndForget);
                Assert.False(db.KeyExists(key));
                Assert.Equal(value, (string)db.ListGetByIndex(key2, 0));

                var tran = db.CreateTransaction();
                var cond = tran.AddCondition(expectEqual ? Condition.ListIndexEqual(key2, 0, expected) : Condition.ListIndexNotEqual(key2, 0, expected));
                var push = tran.ListRightPushAsync(key, "any value");
                var exec = tran.ExecuteAsync();
                var get = db.ListGetByIndex(key, 0);

                Assert.Equal(expectTranResult, db.Wait(exec));
                if (expectEqual == (value == expected))
                {
                    Assert.True(db.Wait(exec), "eq: exec");
                    Assert.True(cond.WasSatisfied, "eq: was satisfied");
                    Assert.Equal(1, db.Wait(push)); // eq: push
                    Assert.Equal("any value", get); // eq: get
                }
                else
                {
                    Assert.False(db.Wait(exec), "neq: exec");
                    Assert.False(cond.WasSatisfied, "neq: was satisfied");
                    Assert.Equal(TaskStatus.Canceled, push.Status); // neq: push
                    Assert.Null((string)get); // neq: get
                }
            }
        }

        public enum ComparisonType
        {
            Equal,
            LessThan,
            GreaterThan
        }

        [Theory]
        [InlineData("five", ComparisonType.Equal, 5L, false)]
        [InlineData("four", ComparisonType.Equal, 4L, true)]
        [InlineData("three", ComparisonType.Equal, 3L, false)]
        [InlineData("", ComparisonType.Equal, 2L, false)]
        [InlineData("", ComparisonType.Equal, 0L, true)]
        [InlineData(null, ComparisonType.Equal, 1L, false)]
        [InlineData(null, ComparisonType.Equal, 0L, true)]

        [InlineData("five", ComparisonType.LessThan, 5L, true)]
        [InlineData("four", ComparisonType.LessThan, 4L, false)]
        [InlineData("three", ComparisonType.LessThan, 3L, false)]
        [InlineData("", ComparisonType.LessThan, 2L, true)]
        [InlineData("", ComparisonType.LessThan, 0L, false)]
        [InlineData(null, ComparisonType.LessThan, 1L, true)]
        [InlineData(null, ComparisonType.LessThan, 0L, false)]

        [InlineData("five", ComparisonType.GreaterThan, 5L, false)]
        [InlineData("four", ComparisonType.GreaterThan, 4L, false)]
        [InlineData("three", ComparisonType.GreaterThan, 3L, true)]
        [InlineData("", ComparisonType.GreaterThan, 2L, false)]
        [InlineData("", ComparisonType.GreaterThan, 0L, false)]
        [InlineData(null, ComparisonType.GreaterThan, 1L, false)]
        [InlineData(null, ComparisonType.GreaterThan, 0L, false)]
        public void BasicTranWithStringLengthCondition(string value, ComparisonType type, long length, bool expectTranResult)
        {
            using (var muxer = Create())
            {
                RedisKey key = Me(), key2 = Me() + "2";
                var db = muxer.GetDatabase();
                db.KeyDelete(key, CommandFlags.FireAndForget);
                db.KeyDelete(key2, CommandFlags.FireAndForget);

                var expectSuccess = false;
                Condition condition = null;
                var valueLength = value?.Length ?? 0;
                switch (type)
                {
                    case ComparisonType.Equal:
                        expectSuccess = valueLength == length;
                        condition = Condition.StringLengthEqual(key2, length);
                        Assert.Contains("String length == " + length, condition.ToString());
                        break;
                    case ComparisonType.GreaterThan:
                        expectSuccess = valueLength > length;
                        condition = Condition.StringLengthGreaterThan(key2, length);
                        Assert.Contains("String length > " + length, condition.ToString());
                        break;
                    case ComparisonType.LessThan:
                        expectSuccess = valueLength < length;
                        condition = Condition.StringLengthLessThan(key2, length);
                        Assert.Contains("String length < " + length, condition.ToString());
                        break;
                }

                if (value != null) db.StringSet(key2, value, flags: CommandFlags.FireAndForget);
                Assert.False(db.KeyExists(key));
                Assert.Equal(value, db.StringGet(key2));

                var tran = db.CreateTransaction();
                var cond = tran.AddCondition(condition);
                var push = tran.StringSetAsync(key, "any value");
                var exec = tran.ExecuteAsync();
                var get = db.StringLength(key);

                Assert.Equal(expectTranResult, db.Wait(exec));

                if (expectSuccess)
                {
                    Assert.True(db.Wait(exec), "eq: exec");
                    Assert.True(cond.WasSatisfied, "eq: was satisfied");
                    Assert.True(db.Wait(push)); // eq: push
                    Assert.Equal("any value".Length, get); // eq: get
                }
                else
                {
                    Assert.False(db.Wait(exec), "neq: exec");
                    Assert.False(cond.WasSatisfied, "neq: was satisfied");
                    Assert.Equal(TaskStatus.Canceled, push.Status); // neq: push
                    Assert.Equal(0, get); // neq: get
                }
            }
        }

        [Theory]
        [InlineData("five", ComparisonType.Equal, 5L, false)]
        [InlineData("four", ComparisonType.Equal, 4L, true)]
        [InlineData("three", ComparisonType.Equal, 3L, false)]
        [InlineData("", ComparisonType.Equal, 2L, false)]
        [InlineData("", ComparisonType.Equal, 0L, true)]

        [InlineData("five", ComparisonType.LessThan, 5L, true)]
        [InlineData("four", ComparisonType.LessThan, 4L, false)]
        [InlineData("three", ComparisonType.LessThan, 3L, false)]
        [InlineData("", ComparisonType.LessThan, 2L, true)]
        [InlineData("", ComparisonType.LessThan, 0L, false)]

        [InlineData("five", ComparisonType.GreaterThan, 5L, false)]
        [InlineData("four", ComparisonType.GreaterThan, 4L, false)]
        [InlineData("three", ComparisonType.GreaterThan, 3L, true)]
        [InlineData("", ComparisonType.GreaterThan, 2L, false)]
        [InlineData("", ComparisonType.GreaterThan, 0L, false)]
        public void BasicTranWithHashLengthCondition(string value, ComparisonType type, long length, bool expectTranResult)
        {
            using (var muxer = Create())
            {
                RedisKey key = Me(), key2 = Me() + "2";
                var db = muxer.GetDatabase();
                db.KeyDelete(key, CommandFlags.FireAndForget);
                db.KeyDelete(key2, CommandFlags.FireAndForget);

                var expectSuccess = false;
                Condition condition = null;
                var valueLength = value?.Length ?? 0;
                switch (type)
                {
                    case ComparisonType.Equal:
                        expectSuccess = valueLength == length;
                        condition = Condition.HashLengthEqual(key2, length);
                        break;
                    case ComparisonType.GreaterThan:
                        expectSuccess = valueLength > length;
                        condition = Condition.HashLengthGreaterThan(key2, length);
                        break;
                    case ComparisonType.LessThan:
                        expectSuccess = valueLength < length;
                        condition = Condition.HashLengthLessThan(key2, length);
                        break;
                }

                for (var i = 0; i < valueLength; i++)
                {
                    db.HashSet(key2, i, value[i].ToString(), flags: CommandFlags.FireAndForget);
                }
                Assert.False(db.KeyExists(key));
                Assert.Equal(valueLength, db.HashLength(key2));

                var tran = db.CreateTransaction();
                var cond = tran.AddCondition(condition);
                var push = tran.StringSetAsync(key, "any value");
                var exec = tran.ExecuteAsync();
                var get = db.StringLength(key);

                Assert.Equal(expectTranResult, db.Wait(exec));

                if (expectSuccess)
                {
                    Assert.True(db.Wait(exec), "eq: exec");
                    Assert.True(cond.WasSatisfied, "eq: was satisfied");
                    Assert.True(db.Wait(push)); // eq: push
                    Assert.Equal("any value".Length, get); // eq: get
                }
                else
                {
                    Assert.False(db.Wait(exec), "neq: exec");
                    Assert.False(cond.WasSatisfied, "neq: was satisfied");
                    Assert.Equal(TaskStatus.Canceled, push.Status); // neq: push
                    Assert.Equal(0, get); // neq: get
                }
            }
        }

        [Theory]
        [InlineData("five", ComparisonType.Equal, 5L, false)]
        [InlineData("four", ComparisonType.Equal, 4L, true)]
        [InlineData("three", ComparisonType.Equal, 3L, false)]
        [InlineData("", ComparisonType.Equal, 2L, false)]
        [InlineData("", ComparisonType.Equal, 0L, true)]

        [InlineData("five", ComparisonType.LessThan, 5L, true)]
        [InlineData("four", ComparisonType.LessThan, 4L, false)]
        [InlineData("three", ComparisonType.LessThan, 3L, false)]
        [InlineData("", ComparisonType.LessThan, 2L, true)]
        [InlineData("", ComparisonType.LessThan, 0L, false)]

        [InlineData("five", ComparisonType.GreaterThan, 5L, false)]
        [InlineData("four", ComparisonType.GreaterThan, 4L, false)]
        [InlineData("three", ComparisonType.GreaterThan, 3L, true)]
        [InlineData("", ComparisonType.GreaterThan, 2L, false)]
        [InlineData("", ComparisonType.GreaterThan, 0L, false)]
        public void BasicTranWithSetCardinalityCondition(string value, ComparisonType type, long length, bool expectTranResult)
        {
            using (var muxer = Create())
            {
                RedisKey key = Me(), key2 = Me() + "2";
                var db = muxer.GetDatabase();
                db.KeyDelete(key, CommandFlags.FireAndForget);
                db.KeyDelete(key2, CommandFlags.FireAndForget);

                var expectSuccess = false;
                Condition condition = null;
                var valueLength = value?.Length ?? 0;
                switch (type)
                {
                    case ComparisonType.Equal:
                        expectSuccess = valueLength == length;
                        condition = Condition.SetLengthEqual(key2, length);
                        break;
                    case ComparisonType.GreaterThan:
                        expectSuccess = valueLength > length;
                        condition = Condition.SetLengthGreaterThan(key2, length);
                        break;
                    case ComparisonType.LessThan:
                        expectSuccess = valueLength < length;
                        condition = Condition.SetLengthLessThan(key2, length);
                        break;
                }

                for (var i = 0; i < valueLength; i++)
                {
                    db.SetAdd(key2, i, flags: CommandFlags.FireAndForget);
                }
                Assert.False(db.KeyExists(key));
                Assert.Equal(valueLength, db.SetLength(key2));

                var tran = db.CreateTransaction();
                var cond = tran.AddCondition(condition);
                var push = tran.StringSetAsync(key, "any value");
                var exec = tran.ExecuteAsync();
                var get = db.StringLength(key);

                Assert.Equal(expectTranResult, db.Wait(exec));

                if (expectSuccess)
                {
                    Assert.True(db.Wait(exec), "eq: exec");
                    Assert.True(cond.WasSatisfied, "eq: was satisfied");
                    Assert.True(db.Wait(push)); // eq: push
                    Assert.Equal("any value".Length, get); // eq: get
                }
                else
                {
                    Assert.False(db.Wait(exec), "neq: exec");
                    Assert.False(cond.WasSatisfied, "neq: was satisfied");
                    Assert.Equal(TaskStatus.Canceled, push.Status); // neq: push
                    Assert.Equal(0, get); // neq: get
                }
            }
        }

        [Theory]
        [InlineData(false, false, true)]
        [InlineData(false, true, false)]
        [InlineData(true, false, false)]
        [InlineData(true, true, true)]
        public void BasicTranWithSetContainsCondition(bool demandKeyExists, bool keyExists, bool expectTranResult)
        {
            using (var muxer = Create(disabledCommands: new[] { "info", "config" }))
            {
                RedisKey key = Me(), key2 = Me() + "2";
                var db = muxer.GetDatabase();
                db.KeyDelete(key, CommandFlags.FireAndForget);
                db.KeyDelete(key2, CommandFlags.FireAndForget);
                RedisValue member = "value";
                if (keyExists) db.SetAdd(key2, member, flags: CommandFlags.FireAndForget);
                Assert.False(db.KeyExists(key));
                Assert.Equal(keyExists, db.SetContains(key2, member));

                var tran = db.CreateTransaction();
                var cond = tran.AddCondition(demandKeyExists ? Condition.SetContains(key2, member) : Condition.SetNotContains(key2, member));
                var incr = tran.StringIncrementAsync(key);
                var exec = tran.ExecuteAsync();
                var get = db.StringGet(key);

                Assert.Equal(expectTranResult, db.Wait(exec));
                if (demandKeyExists == keyExists)
                {
                    Assert.True(db.Wait(exec), "eq: exec");
                    Assert.True(cond.WasSatisfied, "eq: was satisfied");
                    Assert.Equal(1, db.Wait(incr)); // eq: incr
                    Assert.Equal(1, (long)get); // eq: get
                }
                else
                {
                    Assert.False(db.Wait(exec), "neq: exec");
                    Assert.False(cond.WasSatisfied, "neq: was satisfied");
                    Assert.Equal(TaskStatus.Canceled, incr.Status); // neq: incr
                    Assert.Equal(0, (long)get); // neq: get
                }
            }
        }

        [Theory]
        [InlineData("five", ComparisonType.Equal, 5L, false)]
        [InlineData("four", ComparisonType.Equal, 4L, true)]
        [InlineData("three", ComparisonType.Equal, 3L, false)]
        [InlineData("", ComparisonType.Equal, 2L, false)]
        [InlineData("", ComparisonType.Equal, 0L, true)]

        [InlineData("five", ComparisonType.LessThan, 5L, true)]
        [InlineData("four", ComparisonType.LessThan, 4L, false)]
        [InlineData("three", ComparisonType.LessThan, 3L, false)]
        [InlineData("", ComparisonType.LessThan, 2L, true)]
        [InlineData("", ComparisonType.LessThan, 0L, false)]

        [InlineData("five", ComparisonType.GreaterThan, 5L, false)]
        [InlineData("four", ComparisonType.GreaterThan, 4L, false)]
        [InlineData("three", ComparisonType.GreaterThan, 3L, true)]
        [InlineData("", ComparisonType.GreaterThan, 2L, false)]
        [InlineData("", ComparisonType.GreaterThan, 0L, false)]
        public void BasicTranWithSortedSetCardinalityCondition(string value, ComparisonType type, long length, bool expectTranResult)
        {
            using (var muxer = Create())
            {
                RedisKey key = Me(), key2 = Me() + "2";
                var db = muxer.GetDatabase();
                db.KeyDelete(key, CommandFlags.FireAndForget);
                db.KeyDelete(key2, CommandFlags.FireAndForget);

                var expectSuccess = false;
                Condition condition = null;
                var valueLength = value?.Length ?? 0;
                switch (type)
                {
                    case ComparisonType.Equal:
                        expectSuccess = valueLength == length;
                        condition = Condition.SortedSetLengthEqual(key2, length);
                        break;
                    case ComparisonType.GreaterThan:
                        expectSuccess = valueLength > length;
                        condition = Condition.SortedSetLengthGreaterThan(key2, length);
                        break;
                    case ComparisonType.LessThan:
                        expectSuccess = valueLength < length;
                        condition = Condition.SortedSetLengthLessThan(key2, length);
                        break;
                }

                for (var i = 0; i < valueLength; i++)
                {
                    db.SortedSetAdd(key2, i, i, flags: CommandFlags.FireAndForget);
                }
                Assert.False(db.KeyExists(key));
                Assert.Equal(valueLength, db.SortedSetLength(key2));

                var tran = db.CreateTransaction();
                var cond = tran.AddCondition(condition);
                var push = tran.StringSetAsync(key, "any value");
                var exec = tran.ExecuteAsync();
                var get = db.StringLength(key);

                Assert.Equal(expectTranResult, db.Wait(exec));

                if (expectSuccess)
                {
                    Assert.True(db.Wait(exec), "eq: exec");
                    Assert.True(cond.WasSatisfied, "eq: was satisfied");
                    Assert.True(db.Wait(push)); // eq: push
                    Assert.Equal("any value".Length, get); // eq: get
                }
                else
                {
                    Assert.False(db.Wait(exec), "neq: exec");
                    Assert.False(cond.WasSatisfied, "neq: was satisfied");
                    Assert.Equal(TaskStatus.Canceled, push.Status); // neq: push
                    Assert.Equal(0, get); // neq: get
                }
            }
        }

        [Theory]
        [InlineData(false, false, true)]
        [InlineData(false, true, false)]
        [InlineData(true, false, false)]
        [InlineData(true, true, true)]
        public void BasicTranWithSortedSetContainsCondition(bool demandKeyExists, bool keyExists, bool expectTranResult)
        {
            using (var muxer = Create(disabledCommands: new[] { "info", "config" }))
            {
                RedisKey key = Me(), key2 = Me() + "2";
                var db = muxer.GetDatabase();
                db.KeyDelete(key, CommandFlags.FireAndForget);
                db.KeyDelete(key2, CommandFlags.FireAndForget);
                RedisValue member = "value";
                if (keyExists) db.SortedSetAdd(key2, member, 0.0, flags: CommandFlags.FireAndForget);
                Assert.False(db.KeyExists(key));
                Assert.Equal(keyExists, db.SortedSetScore(key2, member).HasValue);

                var tran = db.CreateTransaction();
                var cond = tran.AddCondition(demandKeyExists ? Condition.SortedSetContains(key2, member) : Condition.SortedSetNotContains(key2, member));
                var incr = tran.StringIncrementAsync(key);
                var exec = tran.ExecuteAsync();
                var get = db.StringGet(key);

                Assert.Equal(expectTranResult, db.Wait(exec));
                if (demandKeyExists == keyExists)
                {
                    Assert.True(db.Wait(exec), "eq: exec");
                    Assert.True(cond.WasSatisfied, "eq: was satisfied");
                    Assert.Equal(1, db.Wait(incr)); // eq: incr
                    Assert.Equal(1, (long)get); // eq: get
                }
                else
                {
                    Assert.False(db.Wait(exec), "neq: exec");
                    Assert.False(cond.WasSatisfied, "neq: was satisfied");
                    Assert.Equal(TaskStatus.Canceled, incr.Status); // neq: incr
                    Assert.Equal(0, (long)get); // neq: get
                }
            }
        }

        [Theory]
        [InlineData("five", ComparisonType.Equal, 5L, false)]
        [InlineData("four", ComparisonType.Equal, 4L, true)]
        [InlineData("three", ComparisonType.Equal, 3L, false)]
        [InlineData("", ComparisonType.Equal, 2L, false)]
        [InlineData("", ComparisonType.Equal, 0L, true)]

        [InlineData("five", ComparisonType.LessThan, 5L, true)]
        [InlineData("four", ComparisonType.LessThan, 4L, false)]
        [InlineData("three", ComparisonType.LessThan, 3L, false)]
        [InlineData("", ComparisonType.LessThan, 2L, true)]
        [InlineData("", ComparisonType.LessThan, 0L, false)]

        [InlineData("five", ComparisonType.GreaterThan, 5L, false)]
        [InlineData("four", ComparisonType.GreaterThan, 4L, false)]
        [InlineData("three", ComparisonType.GreaterThan, 3L, true)]
        [InlineData("", ComparisonType.GreaterThan, 2L, false)]
        [InlineData("", ComparisonType.GreaterThan, 0L, false)]
        public void BasicTranWithListLengthCondition(string value, ComparisonType type, long length, bool expectTranResult)
        {
            using (var muxer = Create())
            {
                RedisKey key = Me(), key2 = Me() + "2";
                var db = muxer.GetDatabase();
                db.KeyDelete(key, CommandFlags.FireAndForget);
                db.KeyDelete(key2, CommandFlags.FireAndForget);

                var expectSuccess = false;
                Condition condition = null;
                var valueLength = value?.Length ?? 0;
                switch (type)
                {
                    case ComparisonType.Equal:
                        expectSuccess = valueLength == length;
                        condition = Condition.ListLengthEqual(key2, length);
                        break;
                    case ComparisonType.GreaterThan:
                        expectSuccess = valueLength > length;
                        condition = Condition.ListLengthGreaterThan(key2, length);
                        break;
                    case ComparisonType.LessThan:
                        expectSuccess = valueLength < length;
                        condition = Condition.ListLengthLessThan(key2, length);
                        break;
                }

                for (var i = 0; i < valueLength; i++)
                {
                    db.ListRightPush(key2, i, flags: CommandFlags.FireAndForget);
                }
                Assert.False(db.KeyExists(key));
                Assert.Equal(valueLength, db.ListLength(key2));

                var tran = db.CreateTransaction();
                var cond = tran.AddCondition(condition);
                var push = tran.StringSetAsync(key, "any value");
                var exec = tran.ExecuteAsync();
                var get = db.StringLength(key);

                Assert.Equal(expectTranResult, db.Wait(exec));

                if (expectSuccess)
                {
                    Assert.True(db.Wait(exec), "eq: exec");
                    Assert.True(cond.WasSatisfied, "eq: was satisfied");
                    Assert.True(db.Wait(push)); // eq: push
                    Assert.Equal("any value".Length, get); // eq: get
                }
                else
                {
                    Assert.False(db.Wait(exec), "neq: exec");
                    Assert.False(cond.WasSatisfied, "neq: was satisfied");
                    Assert.Equal(TaskStatus.Canceled, push.Status); // neq: push
                    Assert.Equal(0, get); // neq: get
                }
            }
        }

        [Fact]
        public async Task BasicTran()
        {
            using (var muxer = Create())
            {
                RedisKey key = Me();
                var db = muxer.GetDatabase();
                db.KeyDelete(key, CommandFlags.FireAndForget);
                Assert.False(db.KeyExists(key));

                var tran = db.CreateTransaction();
                var a = tran.StringIncrementAsync(key, 10);
                var b = tran.StringIncrementAsync(key, 5);
                var c = tran.StringGetAsync(key);
                var d = tran.KeyExistsAsync(key);
                var e = tran.KeyDeleteAsync(key);
                var f = tran.KeyExistsAsync(key);
                Assert.False(a.IsCompleted);
                Assert.False(b.IsCompleted);
                Assert.False(c.IsCompleted);
                Assert.False(d.IsCompleted);
                Assert.False(e.IsCompleted);
                Assert.False(f.IsCompleted);
                var result = db.Wait(tran.ExecuteAsync());
                Assert.True(result, "result");
                db.WaitAll(a, b, c, d, e, f);
                Assert.True(a.IsCompleted, "a");
                Assert.True(b.IsCompleted, "b");
                Assert.True(c.IsCompleted, "c");
                Assert.True(d.IsCompleted, "d");
                Assert.True(e.IsCompleted, "e");
                Assert.True(f.IsCompleted, "f");

                var g = db.KeyExists(key);

                Assert.Equal(10, await a.ForAwait());
                Assert.Equal(15, await b.ForAwait());
                Assert.Equal(15, (long)await c.ForAwait());
                Assert.True(await d.ForAwait());
                Assert.True(await e.ForAwait());
                Assert.False(await f.ForAwait());
                Assert.False(g);
            }
        }

        [Fact]
        public void CombineFireAndForgetAndRegularAsyncInTransaction()
        {
            using (var muxer = Create())
            {
                RedisKey key = Me();
                var db = muxer.GetDatabase();
                db.KeyDelete(key, CommandFlags.FireAndForget);
                Assert.False(db.KeyExists(key));

                var tran = db.CreateTransaction("state");
                var a = tran.StringIncrementAsync(key, 5);
                var b = tran.StringIncrementAsync(key, 10, CommandFlags.FireAndForget);
                var c = tran.StringIncrementAsync(key, 15);
                Assert.True(tran.Execute());
                var count = (long)db.StringGet(key);

                Assert.Equal(5, db.Wait(a));
                Assert.Equal("state", a.AsyncState);
                Assert.Equal(0, db.Wait(b));
                Assert.Null(b.AsyncState);
                Assert.Equal(30, db.Wait(c));
                Assert.Equal("state", a.AsyncState);
                Assert.Equal(30, count);
            }
        }
    }
}