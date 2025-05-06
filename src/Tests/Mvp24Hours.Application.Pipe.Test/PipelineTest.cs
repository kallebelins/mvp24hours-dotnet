//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Application.Pipe.Test.Operations;
using Mvp24Hours.Application.Pipe.Test.Rollbacks;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Pipe;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;
using Xunit.Priority;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Mvp24Hours.Application.Pipe.Test
{
    /// <summary>
    /// 
    /// </summary>
    [TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Name)]
    public class PipelineTest
    {
        [Fact, Priority(1)]
        public void PipelineStarted()
        {
            // arrange
            Pipeline pipeline = new();

            // act
            pipeline.Add(_ =>
            {
                Trace.WriteLine("Test 1");
            });
            pipeline.Add(_ =>
            {
                Trace.WriteLine("Test 2");
            });
            pipeline.Add(_ =>
            {
                Trace.WriteLine("Test 3");
            });
            pipeline.Execute();

            // assert
            Assert.NotNull(pipeline.GetMessage());
        }

        [Fact, Priority(2)]
        public void PipelineMessageContentGet()
        {
            // arrange
            Pipeline pipeline = new();

            // act

            // add operations
            pipeline.Add(input =>
            {
                string param = input.GetContent<string>();
                Trace.WriteLine($"Test 1 - {param}");
            });
            pipeline.Add(input =>
            {
                string param = input.GetContent<string>();
                Trace.WriteLine($"Test 2 - {param}");
            });
            pipeline.Add(input =>
            {
                string param = input.GetContent<string>();
                Trace.WriteLine($"Test 3 - {param}");
            });

            // define param
            var message = "Parameter received.".ToMessage();

            pipeline.Execute(message);

            // assert
            Assert.NotNull(pipeline.GetMessage());
        }

        [Fact, Priority(3)]
        public void PipelineMessageContentAdd()
        {
            // arrange
            Pipeline pipeline = new();

            // act

            // add operations
            pipeline.Add(input =>
            {
                string param = input.GetContent<string>();
                input.AddContent("teste1", $"Test 1 - {param}");
            });
            pipeline.Add(input =>
            {
                string param = input.GetContent<string>();
                input.AddContent("teste2", $"Test 2 - {param}");
            });
            pipeline.Add(input =>
            {
                string param = input.GetContent<string>();
                input.AddContent("teste3", $"Test 3 - {param}");
            });

            // define attachment for message 
            var message = "Parameter received.".ToMessage();

            pipeline.Execute(message);

            // get content from result
            foreach (var item in pipeline.GetMessage().GetContentAll())
            {
                if (item is string)
                {
                    Trace.WriteLine(item);
                }
            }

            // assert
            Assert.NotNull(pipeline.GetMessage());
        }

        [Fact, Priority(4)]
        public void PipelineMessageContentValidate()
        {
            // arrange
            Pipeline pipeline = new();

            // act

            // add operations
            pipeline.Add(input =>
            {
                if (input.HasContent<string>())
                {
                    string param = input.GetContent<string>();
                    Trace.WriteLine($"Content - {param}");
                }
                else
                {
                    Trace.WriteLine("Content not found");
                }
            });
            pipeline.Execute();
            var result1 = pipeline.GetMessage();
            pipeline.Execute("Parameter received.".ToMessage());
            var result2 = pipeline.GetMessage();

            // assert
            Assert.True(result1 != null && result2 != null);
        }

        [Fact, Priority(5)]
        public void PipelineOperationLock()
        {
            // arrange
            Pipeline pipeline = new();

            // act

            // add operations
            pipeline.Add(input =>
            {
                string param = input.GetContent<string>();
                Trace.WriteLine($"Test 1 - {param}");
            });
            pipeline.Add(input =>
            {
                string param = input.GetContent<string>();
                Trace.WriteLine($"Test 2 - {param}");
                Trace.WriteLine($"Locking....");
                input.SetLock();
            });
            pipeline.Add(input =>
            {
                string param = input.GetContent<string>();
                Trace.WriteLine($"Test 3 - {param}");
            });

            pipeline.Execute("Parameter received.".ToMessage());

            // assert
            Assert.NotNull(pipeline.GetMessage());
        }

        [Fact, Priority(5)]
        public void PipelineOperationLockExecuteForce()
        {
            // arrange
            Pipeline pipeline = new();

            // act

            // add operations
            pipeline.Add(input =>
            {
                string param = input.GetContent<string>();
                Trace.WriteLine($"Test 1 - {param}");
            });
            pipeline.Add(input =>
            {
                string param = input.GetContent<string>();
                Trace.WriteLine($"Test 2 - {param}");
                Trace.WriteLine($"Locking....");
                input.SetLock();
            });
            pipeline.Add(input =>
            {
                string param = input.GetContent<string>();
                Trace.WriteLine($"Test 3 - {param}");
            });
            pipeline.Add(input =>
            {
                string param = input.GetContent<string>();
                Trace.WriteLine($"Required - {param}");
            }, true);
            pipeline.Add(input =>
            {
                string param = input.GetContent<string>();
                Trace.WriteLine($"Test 4 - {param}");
            });

            // interceptors -> locked-operation
            pipeline.AddInterceptors(_ =>
            {
                Trace.WriteLine("Locked-Operation, only one time.");
            }, PipelineInterceptorType.Locked);

            pipeline.Execute("Parameter received.".ToMessage());

            // assert
            Assert.NotNull(pipeline.GetMessage());
        }

        [Fact, Priority(6)]
        public void PipelineOperationFailure()
        {
            // arrange
            Pipeline pipeline = new();

            // act

            // add operations
            pipeline.Add(input =>
            {
                string param = input.GetContent<string>();
                Trace.WriteLine($"Test 1 - {param}");
            });
            pipeline.Add(input =>
            {
                string param = input.GetContent<string>();
                Trace.WriteLine($"Test 2 - {param}");
                Trace.WriteLine($"Failure....");
                input.SetFailure();
            });
            pipeline.Add(input =>
            {
                string param = input.GetContent<string>();
                Trace.WriteLine($"Test 3 - {param}");
            });

            // interceptors -> locked-operation
            pipeline.AddInterceptors(_ =>
            {
                Trace.WriteLine("Faulty-Operation, only one time.");
            }, PipelineInterceptorType.Faulty);

            pipeline.Execute("Parameter received.".ToMessage());

            // assert
            Assert.NotNull(pipeline.GetMessage());
        }

        [Fact, Priority(7)]
        public void PipelineOperationLockWithNotification()
        {
            // arrange
            Pipeline pipeline = new();

            // act

            // add operations
            pipeline.Add(input =>
            {
                input.Messages.AddMessage("Content not found", Core.Enums.MessageType.Error);
            });
            pipeline.Add(input =>
            {
                Trace.WriteLine("Operation blocked by 'Error' notification.");
            });

            // interceptors -> locked-operation
            pipeline.AddInterceptors(_ =>
            {
                Trace.WriteLine("Locked-Operation, only one time.");
            }, PipelineInterceptorType.Locked);

            pipeline.Execute();

            var message = pipeline.GetMessage();

            foreach (var item in message.Messages)
            {
                Trace.WriteLine(item.Message);
            }

            // assert
            Assert.True(message.IsFaulty);
        }

        [Fact, Priority(8)]
        public void PipelineInterceptors()
        {
            // arrange
            Pipeline pipeline = new();

            // act

            // operations
            pipeline.Add(_ =>
            {
                Trace.WriteLine("Test 1");
            });
            pipeline.Add(input =>
            {
                Trace.WriteLine("Test 2");
                Trace.WriteLine("Adding value to conditional interceptor test...");
                input.AddContent(1);
            });
            pipeline.Add(_ =>
            {
                Trace.WriteLine("Test 3");
            });

            // interceptors -> first-operation
            pipeline.AddInterceptors(_ =>
            {
                Trace.WriteLine("First-Operation, only one time.");
            }, PipelineInterceptorType.FirstOperation);

            // interceptors -> pre-operation
            pipeline.AddInterceptors(_ =>
            {
                Trace.WriteLine("Pre-Operation");
            }, PipelineInterceptorType.PreOperation);

            // interceptors -> post-operation
            pipeline.AddInterceptors(_ =>
            {
                Trace.WriteLine("Post-Operation");
            }, PipelineInterceptorType.PostOperation);

            // interceptors -> last-operation
            pipeline.AddInterceptors(_ =>
            {
                Trace.WriteLine("Last-Operation, only one time.");
            }, PipelineInterceptorType.LastOperation);

            // interceptors -> locked-operation
            pipeline.AddInterceptors(_ =>
            {
                Trace.WriteLine("Locked-Operation, only one time.");
            }, PipelineInterceptorType.Locked);

            // interceptors -> faulty-operation
            pipeline.AddInterceptors(_ =>
            {
                Trace.WriteLine("Faulty-Operation, only one time.");
            }, PipelineInterceptorType.Faulty);

            // interceptors -> conditional
            pipeline.AddInterceptors(_ =>
            {
                Trace.WriteLine("Conditional-Operation.");
            },
            input =>
            {
                return input.HasContent<int>();
            });

            pipeline.Execute();

            // assert
            Assert.NotNull(pipeline.GetMessage());
        }

        [Fact, Priority(9)]
        public void PipelineEventInterceptors()
        {
            // arrange
            Pipeline pipeline = new();

            // act

            // operations
            pipeline.Add(_ =>
            {
                Trace.WriteLine("Test 1");
            });
            pipeline.Add(input =>
            {
                Trace.WriteLine("Test 2");
                Trace.WriteLine("Adding value to conditional interceptor test...");
                input.AddContent(1);
            });
            pipeline.Add(_ =>
            {
                Trace.WriteLine("Test 3");
            });

            // event interceptors -> first-operation
            pipeline.AddInterceptors((input, e) =>
            {
                Trace.WriteLine("First-Operation, event.");
            }, PipelineInterceptorType.FirstOperation);

            // event interceptors -> pre-operation
            pipeline.AddInterceptors((input, e) =>
            {
                Trace.WriteLine("Pre-Operation, event.");
            }, PipelineInterceptorType.PreOperation);

            // event interceptors -> post-operation
            pipeline.AddInterceptors((input, e) =>
            {
                Trace.WriteLine("Post-Operation, event.");
            }, PipelineInterceptorType.PostOperation);

            // event interceptors -> last-operation
            pipeline.AddInterceptors((input, e) =>
            {
                Trace.WriteLine("Last-Operation, event.");
            }, PipelineInterceptorType.LastOperation);

            // event interceptors -> locked-operation
            pipeline.AddInterceptors((input, e) =>
            {
                Trace.WriteLine("Locked-Operation, event.");
            }, PipelineInterceptorType.Locked);

            // event interceptors -> faulty-operation
            pipeline.AddInterceptors((input, e) =>
            {
                Trace.WriteLine("Faulty-Operation, event.");
            }, PipelineInterceptorType.Faulty);

            // event interceptors -> conditional
            pipeline.AddInterceptors((input, e) =>
            {
                Trace.WriteLine("Conditional-Operation, event.");
            },
            input =>
            {
                return input.HasContent<int>();
            });

            pipeline.Execute();

            // assert
            Assert.NotNull(pipeline.GetMessage());
        }

        [Fact, Priority(10)]
        public void PipelineEventInterceptorsWithLock()
        {
            // arrange
            Pipeline pipeline = new();

            // act

            // operations
            pipeline.Add(_ =>
            {
                Trace.WriteLine("Test 1");
            });
            pipeline.Add(input =>
            {
                Trace.WriteLine("Test 2");
                Trace.WriteLine("Adding value to conditional interceptor test...");
                input.AddContent(1);
            });
            pipeline.Add(input =>
            {
                Trace.WriteLine("Test 3");
                Trace.WriteLine("Locking...");
                input.SetLock();
            });

            // event interceptors -> first-operation
            pipeline.AddInterceptors((input, e) =>
            {
                Trace.WriteLine("First-Operation, event.");
            }, PipelineInterceptorType.FirstOperation);

            // event interceptors -> pre-operation
            pipeline.AddInterceptors((input, e) =>
            {
                Trace.WriteLine("Pre-Operation, event.");
            }, PipelineInterceptorType.PreOperation);

            // event interceptors -> post-operation
            pipeline.AddInterceptors((input, e) =>
            {
                Trace.WriteLine("Post-Operation, event.");
            }, PipelineInterceptorType.PostOperation);

            // event interceptors -> last-operation
            pipeline.AddInterceptors((input, e) =>
            {
                Trace.WriteLine("Last-Operation, event.");
            }, PipelineInterceptorType.LastOperation);

            // event interceptors -> locked-operation
            pipeline.AddInterceptors((input, e) =>
            {
                Trace.WriteLine("Locked-Operation, event.");
            }, PipelineInterceptorType.Locked);

            // event interceptors -> faulty-operation
            pipeline.AddInterceptors((input, e) =>
            {
                Trace.WriteLine("Faulty-Operation, event.");
            }, PipelineInterceptorType.Faulty);

            // event interceptors -> conditional
            pipeline.AddInterceptors((input, e) =>
            {
                Trace.WriteLine("Conditional-Operation, event.");
            },
            input =>
            {
                return input.HasContent<int>();
            });

            pipeline.Execute();

            // assert
            Assert.NotNull(pipeline.GetMessage());
        }

        [Fact, Priority(11)]
        public void PipelineWithOperation()
        {
            // arrange
            Pipeline pipeline = new();

            // act
            pipeline.Add<OperationTest>();

            // operations
            pipeline.Execute();
            var result = pipeline.GetMessage().GetContent<int>("key-test");

            // assert
            Assert.Equal(1, result);
        }

        [Fact, Priority(12)]
        public void PipelineMessageWithError()
        {
            // arrange
            Pipeline pipeline = new();

            // act
            pipeline.Add(input =>
            {
                input.AddError("minha mensagem de erro");
            });

            // operations
            pipeline.Execute();
            var pipelineMessage = pipeline.GetMessage();

            // assert
            Assert.True(pipelineMessage.IsFaulty);
            Assert.Single(pipelineMessage.Messages);
            Assert.Equal(MessageType.Error, pipelineMessage.Messages[0].Type);
        }

        [Fact, Priority(13)]
        public void PipelineWithRollbackOperations()
        {
            // arrange
            Pipeline pipeline = new() { ForceRollbackOnFalure = true };
            RollbackTestContext.Results.Clear();

            // act
            pipeline.Add<RollbackOperationTestStep1>();
            pipeline.Add<RollbackOperationTestStep2>();
            pipeline.Add<RollbackOperationTestStep3>();

            // operations
            pipeline.Execute();
            var resultExecutionStep1 = pipeline.GetMessage().GetContent<int>("key-test-step1");
            var resultExecutionStep2 = pipeline.GetMessage().GetContent<int>("key-test-step2");
            var resultExecutionStep3 = pipeline.GetMessage().GetContent<int>("key-test-step3");
            var resultRollbackStep1 = pipeline.GetMessage().GetContent<int>("key-test-rollback-step1");
            var resultRollbackStep2 = pipeline.GetMessage().GetContent<int>("key-test-rollback-step2");
            var resultRollbackStep3 = pipeline.GetMessage().HasContent("key-test-rollback-step3");

            var resultIndexStep1 = RollbackTestContext.Results.IndexOf("key-test-rollback-step1");
            var resultIndexStep2 = RollbackTestContext.Results.IndexOf("key-test-rollback-step2");

            // assert
            Assert.Equal(1, resultExecutionStep1);
            Assert.Equal(10, resultRollbackStep1);
            Assert.Equal(2, resultExecutionStep2);
            Assert.Equal(20, resultRollbackStep2);
            Assert.Equal(3, resultExecutionStep3);
            Assert.False(resultRollbackStep3);

            Assert.Equal(1, resultIndexStep1); //Step 1 must be after step 2 because all rollbacks are executed top-down
            Assert.Equal(0, resultIndexStep2);
        }

        [Fact, Priority(14)]
        public void PipelineWithRollbackOperationsWithoutForceRollbackOnFalure()
        {
            // arrange
            Pipeline pipeline = new() { ForceRollbackOnFalure = false };

            // act
            pipeline.Add<RollbackOperationTestStep1>();
            pipeline.Add<RollbackOperationTestStep2>();
            pipeline.Add<RollbackOperationTestStep3>();

            // operations
            pipeline.Execute();
            var resultExecutionStep1 = pipeline.GetMessage().GetContent<int>("key-test-step1");
            var resultExecutionStep2 = pipeline.GetMessage().GetContent<int>("key-test-step2");
            var resultExecutionStep3 = pipeline.GetMessage().GetContent<int>("key-test-step3");
            var resultRollbackStep1 = pipeline.GetMessage().HasContent("key-test-rollback-step1");
            var resultRollbackStep2 = pipeline.GetMessage().HasContent("key-test-rollback-step2");
            var resultRollbackStep3 = pipeline.GetMessage().HasContent("key-test-rollback-step3");

            // assert
            Assert.Equal(1, resultExecutionStep1);
            Assert.Equal(2, resultExecutionStep2);
            Assert.Equal(3, resultExecutionStep3);
            Assert.False(resultRollbackStep1);
            Assert.False(resultRollbackStep2);
            Assert.False(resultRollbackStep3);
        }

        [Fact, Priority(15)]
        public void PipelineWithWithAllowPropagateException()
        {
            // arrange
            var pipeline = new Pipeline() { AllowPropagateException = true };
            var exception = default(Exception);

            // act
            pipeline.Add<RollbackOperationTestStep1>();
            pipeline.Add<RollbackOperationTestStep2>();
            pipeline.Add<RollbackOperationTestStep3>();

            try
            {
                // operations
                pipeline.Execute();
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            // assert
            Assert.NotNull(exception);
            Assert.Equal("My Exception 123", exception.Message);
            Assert.Equal(typeof(InvalidOperationException), exception.GetType());
        }

        [Fact, Priority(16)]
        public void PipelineWithWithoutAllowPropagateException()
        {
            // arrange
            var pipeline = new Pipeline(); //AllowPropagateException = false
            var exception = default(Exception);

            // act
            pipeline.Add<RollbackOperationTestStep1>();
            pipeline.Add<RollbackOperationTestStep2>();
            pipeline.Add<RollbackOperationTestStep3>();

            try
            {
                // operations
                pipeline.Execute();
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            // assert
            Assert.Null(exception);
        }

        [Fact, Priority(17)]
        public void PipelineMessageUsingNonNullableContents()
        {
            // arrange
            IPipelineMessage input = new PipelineMessage();

            // operations
            input.DynamicContents.Person = new Person { Name = "John Smith", CC = new CC { Number = "4532849103927456", CVV = "435", ExpirationDate = "11/32" } };
            input.AddContent("person_name", input.DynamicContents.Person.Name);
            input.AddContent("person_CC_Number", input.DynamicContents.Person.CC.Number);
            input.AddContent("person_CC_CVV", input.DynamicContents.Person.CC.CVV);
            input.AddContent("person_CC_ExpirationDate", input.DynamicContents.Person.CC.ExpirationDate);
            input.AddContent("person_CC_ExpirationDate", input.DynamicContents.Person.CC.ExpirationDate);

            // assert
            Assert.Equal(input.DynamicContents.Person.Name, "John Smith");
            Assert.Equal(input.DynamicContents.Person.CC.Number, "4532849103927456");
            Assert.Equal(input.DynamicContents.Person.CC.CVV, "435");
            Assert.Equal(input.DynamicContents.Person.CC.ExpirationDate, "11/32");

            Assert.Equal(input.DynamicContents.Person.Name, input.GetContent<string>("person_name"));
            Assert.Equal(input.DynamicContents.Person.CC.Number, input.GetContent<string>("person_CC_Number"));
            Assert.Equal(input.DynamicContents.Person.CC.CVV, input.GetContent<string>("person_CC_CVV"));
            Assert.Equal(input.DynamicContents.Person.CC.ExpirationDate, input.GetContent<string>("person_CC_ExpirationDate"));

            Assert.Equal(input.DynamicContents.person_name, input.GetContent<string>("person_name"));
            Assert.Equal(input.DynamicContents.person_CC_Number, input.GetContent<string>("person_CC_Number"));
            Assert.Equal(input.DynamicContents.person_CC_CVV, input.GetContent<string>("person_CC_CVV"));
            Assert.Equal(input.DynamicContents.person_CC_ExpirationDate, input.GetContent<string>("person_CC_ExpirationDate"));

            var personExpected = input.GetContent<Person>("Person");
            var personActual = input.DynamicContents.Person;

            Assert.Equal(personExpected.Name, personActual.Name);
            Assert.Equal(personExpected.CC.Number, personActual.CC.Number);
            Assert.Equal(personExpected.CC.CVV, personActual.CC.CVV);
            Assert.Equal(personExpected.CC.ExpirationDate, personActual.CC.ExpirationDate);
        }

        [Fact, Priority(18)]
        public void PipelineMessageUsingNonNullableContentsWithNullValues()
        {
            // arrange
            IPipelineMessage input = new PipelineMessage();
            ArgumentNullException setExceptionNull = default;
            ArgumentOutOfRangeException getExceptionOutOfRange = default;

            // operations
            try
            {
                input.DynamicContents.Person = default(Person);
            }
            catch (ArgumentNullException ex)
            {
                setExceptionNull = ex;
            }

            try
            {
                var person = input.DynamicContents.PersonNotExist;
            }
            catch (ArgumentOutOfRangeException ex)
            {
                getExceptionOutOfRange = ex;
            }

            // assert
            Assert.NotNull(setExceptionNull);
            Assert.NotNull(getExceptionOutOfRange);
        }

        class Person
        {
            public string Name { get; set; }
            public CC CC { get; set; }
        }

        class CC
        {
            public string Number { get; set; }
            public string CVV { get; set; }
            public string ExpirationDate { get; set; }
        }
    }
}
