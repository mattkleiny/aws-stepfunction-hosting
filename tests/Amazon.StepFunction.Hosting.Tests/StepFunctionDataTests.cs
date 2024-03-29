﻿using System;
using NUnit.Framework;

namespace Amazon.StepFunction.Hosting
{
  public class StepFunctionDataTests
  {
    [Test]
    public void it_should_convert_bool_to_and_from_data()
    {
      var data = new StepFunctionData(true);
      var raw  = data.Cast<bool>();

      Assert.AreEqual(true, raw);
    }

    [Test]
    public void it_should_convert_int_to_and_from_data()
    {
      var data = new StepFunctionData(42);
      var raw  = data.Cast<int>();

      Assert.AreEqual(42, raw);
    }

    [Test]
    public void it_should_convert_float_to_and_from_data()
    {
      var data = new StepFunctionData(MathF.PI);
      var raw  = data.Cast<float>();

      Assert.AreEqual(MathF.PI, raw);
    }

    [Test]
    public void it_should_convert_TimeSpan_to_and_from_data()
    {
      var timeSpan = DateTime.Now.TimeOfDay;

      var data = new StepFunctionData(timeSpan);
      var raw  = data.Cast<TimeSpan>();

      Assert.AreEqual(timeSpan, raw);
    }

    [Test]
    public void it_should_convert_DateTime_to_and_from_data()
    {
      var dateTime = DateTime.Now;

      var data = new StepFunctionData(dateTime);
      var raw  = data.Cast<DateTime>();

      Assert.AreEqual(dateTime, raw);
    }

    [Test]
    public void it_should_convert_string_to_and_from_data()
    {
      var data = new StepFunctionData("Hello, World!");
      var raw  = data.Cast<string>();

      Assert.AreEqual("Hello, World!", raw);
    }

    [Test]
    public void it_should_permit_querying_properties_in_JPath_expression()
    {
      var expectedMessage   = "Hello, World!";
      var expectedTimestamp = new DateTime(1988, 09, 03);

      var data = new StepFunctionData(new
      {
        Message   = expectedMessage,
        Timestamp = expectedTimestamp
      });

      Assert.AreEqual(expectedMessage, data.Query("$.Message").Cast<string>());
      Assert.AreEqual(expectedTimestamp, data.Query("$.Timestamp").Cast<DateTime>());
    }

    [Test]
    public void it_should_compare_by_value_equality()
    {
      var data1 = new StepFunctionData(new
      {
        SubObject = new
        {
          Message   = "Test Message 1",
          Timestamp = new TimeSpan(10, 00, 00)
        }
      });

      var data2 = new StepFunctionData(new
      {
        Message   = "Test Message 1",
        Timestamp = new TimeSpan(10, 00, 00)
      });

      Assert.AreEqual(data2, data1.Query("$.SubObject"));
    }

    [Test]
    public void it_should_expand_context_object_into_values()
    {
      var input = new StepFunctionData(new
      {
        Message = "Hello, World"
      });

      var result = input.Transform(
        jpath: "$.Context",
        template: new StepFunctionData(new
        {
          ExecutionId = "$$.ExecutionId",
          TaskToken   = "$$.TaskToken"
        }),
        context: new StepFunctionData(new
        {
          ExecutionId = Guid.NewGuid(),
          TaskToken   = Guid.NewGuid()
        })
      );

      Assert.IsNotNull(result.Query("$.Message").Cast<string>());
      Assert.IsNotNull(result.Query("$.Context.ExecutionId").Cast<string>());
      Assert.IsNotNull(result.Query("$.Context.TaskToken").Cast<string>());
    }
  }
}