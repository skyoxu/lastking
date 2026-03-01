using System;
using System.Text.Json;
using FluentAssertions;
using Game.Core.Contracts;
using Xunit;

namespace Game.Core.Tests.Contracts;

public class DomainEventPayloadTests
{
    [Fact]
    public void Create_should_serialize_typed_payload_to_json_element()
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
    public void Legacy_constructor_should_keep_data_and_generate_data_element()
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
    public void String_json_payload_should_be_parsed_as_json_object()
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
    public void DeserializeData_should_roundtrip_payload()
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

    private sealed record DamagePayload(int Amount, string Type, bool Critical);
}
