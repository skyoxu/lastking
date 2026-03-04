using System;
using System.Text.Json;
using FluentAssertions;
using Game.Core.Contracts;
using Xunit;

namespace Game.Core.Tests.Contracts;

public class DomainEventPayloadTests
{
    [Fact]
    public void ShouldSerializeTypedPayloadToJsonElement_WhenCreateCalled()
    {
        var payload = new DamagePayload(123, "Fire", true);
        var evt = DomainEvent.Create(
            type: "core.test.damage.applied",
            source: nameof(DomainEventPayloadTests),
            payload: payload,
            timestamp: DateTime.UtcNow,
            id: Guid.NewGuid().ToString("N")
        );

        evt.DataElement.HasValue.Should().BeTrue();
        evt.DataJson.Should().NotBeNullOrWhiteSpace();

        var node = evt.DataElement!.Value;
        node.GetProperty("Amount").GetInt32().Should().Be(123);
        node.GetProperty("Type").GetString().Should().Be("Fire");
        node.GetProperty("Critical").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public void ShouldKeepDataAndGenerateDataElement_WhenLegacyConstructorUsed()
    {
#pragma warning disable CS0618
        var evt = new DomainEvent(
            Type: "core.test.legacy.created",
            Source: nameof(DomainEventPayloadTests),
            Data: new DamagePayload(99, "Ice", false),
            Timestamp: DateTime.UtcNow,
            Id: Guid.NewGuid().ToString("N")
        );

        evt.Data.Should().NotBeNull();
#pragma warning restore CS0618
        evt.DataElement.HasValue.Should().BeTrue();
        evt.DataElement!.Value.GetProperty("Amount").GetInt32().Should().Be(99);
    }

    [Fact]
    public void ShouldParseJsonObject_WhenPayloadStringIsJson()
    {
        var evt = new DomainEvent(
            Type: "core.test.json.payload",
            Source: nameof(DomainEventPayloadTests),
            Data: "{\"kind\":\"spawn\",\"count\":5}",
            Timestamp: DateTime.UtcNow,
            Id: Guid.NewGuid().ToString("N")
        );

        evt.DataElement.HasValue.Should().BeTrue();
        evt.DataElement!.Value.GetProperty("kind").GetString().Should().Be("spawn");
        evt.DataElement!.Value.GetProperty("count").GetInt32().Should().Be(5);
    }

    [Fact]
    public void ShouldRoundtripPayload_WhenDeserializeDataCalled()
    {
        var payload = new DamagePayload(12, "Arcane", false);
        var evt = DomainEvent.Create(
            type: "core.test.payload.roundtrip",
            source: nameof(DomainEventPayloadTests),
            payload: payload,
            timestamp: DateTime.UtcNow,
            id: Guid.NewGuid().ToString("N")
        );

        var back = evt.DeserializeData<DamagePayload>();
        back.Should().NotBeNull();
        back!.Amount.Should().Be(12);
        back.Type.Should().Be("Arcane");
        back.Critical.Should().BeFalse();
    }

    [Fact]
    public void ShouldHandleNullPayloadAndDeserializeDefault_WhenCreateCalled()
    {
        var evt = DomainEvent.Create<object?>(
            type: "core.test.null.payload",
            source: nameof(DomainEventPayloadTests),
            payload: null,
            timestamp: DateTime.UtcNow,
            id: Guid.NewGuid().ToString("N")
        );

        evt.DataElement.HasValue.Should().BeFalse();
        evt.DataJson.Should().Be("{}");
        evt.DeserializeData<DamagePayload>().Should().BeNull();
    }

    [Fact]
    public void ShouldHandleWhitespaceAndInvalidJson_WhenConstructorGivenPayloadString()
    {
        var white = new DomainEvent(
            Type: "core.test.whitespace.payload",
            Source: nameof(DomainEventPayloadTests),
            Data: "   ",
            Timestamp: DateTime.UtcNow,
            Id: Guid.NewGuid().ToString("N")
        );
        var invalid = new DomainEvent(
            Type: "core.test.invalid.payload",
            Source: nameof(DomainEventPayloadTests),
            Data: "{bad_json",
            Timestamp: DateTime.UtcNow,
            Id: Guid.NewGuid().ToString("N")
        );

        white.DataElement.HasValue.Should().BeTrue();
        white.DataElement!.Value.ValueKind.Should().Be(JsonValueKind.Object);
        invalid.DataElement.HasValue.Should().BeTrue();
        invalid.DataElement!.Value.ValueKind.Should().Be(JsonValueKind.String);
    }

    [Fact]
    public void ShouldCloneJsonElementPayload_WhenConstructorReceivesPayload()
    {
        using var doc = JsonDocument.Parse("{\"hp\":100}");
        var evt = new DomainEvent(
            Type: "core.test.element.payload",
            Source: nameof(DomainEventPayloadTests),
            Data: doc.RootElement,
            Timestamp: DateTime.UtcNow,
            Id: Guid.NewGuid().ToString("N")
        );

        evt.DataElement.HasValue.Should().BeTrue();
        evt.DataElement!.Value.GetProperty("hp").GetInt32().Should().Be(100);
    }

    private sealed record DamagePayload(int Amount, string Type, bool Critical);
}
