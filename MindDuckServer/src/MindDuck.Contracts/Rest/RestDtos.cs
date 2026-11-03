// Bodies of the REST endpoints (/api/...). See protocol.md section 7.

using System;

namespace MindDuck.Contracts
{
    /// <summary>POST /api/rooms -> 201</summary>
    public sealed class CreateRoomResponse
    {
        public string RoomCode { get; set; }
    }

    /// <summary>GET /api/rooms/{code} -> 200 (or 404)</summary>
    public sealed class RoomInfoDto
    {
        public string RoomCode { get; set; }
        public RoomStatus Status { get; set; }
        public int Players { get; set; }
    }

    /// <summary>GET /api/matches?limit=20 -> list, newest first. (Draft: confirm fields with P5's database tables.)</summary>
    public sealed class MatchSummaryDto
    {
        public string MatchId { get; set; }
        public DateTime StartedAtUtc { get; set; }
        public DateTime? EndedAtUtc { get; set; }
        public string P1Nickname { get; set; }
        public string P2Nickname { get; set; }
        public PlayerSeat? Winner { get; set; }
        public MatchEndReason? EndReason { get; set; }
        public int TurnCount { get; set; }
    }

    /// <summary>GET /api/matches/{id}/events -> list. One logged WebSocket message of that match.</summary>
    public sealed class MatchEventDto
    {
        public int Sequence { get; set; }
        /// <summary>"in" = sent by a player, "out" = sent by the server.</summary>
        public string Kind { get; set; }
        public PlayerSeat? Seat { get; set; }
        /// <summary>Message type name, e.g. "PlayCard".</summary>
        public string Type { get; set; }
        /// <summary>The message payload as stored (jsonb). Arrives as a JSON object.</summary>
        public object Payload { get; set; }
    }

    /// <summary>GET /api/health -> 200 { "status": "ok" }</summary>
    public sealed class HealthResponse
    {
        public string Status { get; set; }
    }

    /// <summary>Body of every REST error (4xx/5xx).</summary>
    public sealed class ErrorResponse
    {
        public ErrorCode Code { get; set; }
        public string Message { get; set; }
    }
}
