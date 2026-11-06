using System.Text;
using System.Text.RegularExpressions;

namespace SecretSpots.Features.Assistant;

// Removes the [spot:id] markers (and the whitespace before them) from answer text that arrives in
// pieces, so a marker split across pieces ("... falls [sp" + "ot:ab12]") never reaches the user.
// Text that might still turn into a marker is held back until the next piece settles it; trailing
// whitespace is held too, in case a marker follows.
public sealed partial class SpotMarkerStreamFilter
{
    private const string MarkerStart = "[spot:";

    // Longer than any real marker (a 36-char id plus "[spot:" and "]"): past this it's ordinary text.
    private const int MaxMarkerLength = 64;

    private readonly StringBuilder _pending = new();

    [GeneratedRegex(@"^\[spot:\s*[0-9a-fA-F-]+\s*\]$")]
    private static partial Regex CompleteMarker();

    [GeneratedRegex(@"^\[spot:\s*[0-9a-fA-F-]*\s*$")]
    private static partial Regex UnfinishedMarker();

    // Returns the text that's safe to show now (possibly empty).
    public string Push(string piece)
    {
        _pending.Append(piece);
        var buffer = _pending.ToString();
        var output = new StringBuilder();

        while (true)
        {
            var open = buffer.IndexOf('[');
            if (open < 0)
            {
                var keep = TrailingWhitespaceStart(buffer, buffer.Length);
                output.Append(buffer, 0, keep);
                buffer = buffer[keep..];
                break;
            }

            var close = buffer.IndexOf(']', open);
            if (close < 0)
            {
                if (CouldStillBeMarker(buffer[open..]))
                {
                    var keep = TrailingWhitespaceStart(buffer, open);
                    output.Append(buffer, 0, keep);
                    buffer = buffer[keep..];
                    break;
                }

                output.Append(buffer, 0, open + 1);
                buffer = buffer[(open + 1)..];
                continue;
            }

            if (CompleteMarker().IsMatch(buffer[open..(close + 1)]))
            {
                output.Append(buffer, 0, TrailingWhitespaceStart(buffer, open));
                buffer = buffer[(close + 1)..];
            }
            else
            {
                output.Append(buffer, 0, open + 1);
                buffer = buffer[(open + 1)..];
            }
        }

        _pending.Clear().Append(buffer);
        return output.ToString();
    }

    // Whatever was held back, once no more text is coming (an unfinished marker is shown as written).
    public string Flush()
    {
        var rest = _pending.ToString();
        _pending.Clear();
        return rest;
    }

    private static bool CouldStillBeMarker(string tail) =>
        tail.Length <= MaxMarkerLength
        && (MarkerStart.StartsWith(tail, StringComparison.Ordinal) || UnfinishedMarker().IsMatch(tail));

    // Where the run of whitespace ending at `end` begins (end itself when there is none).
    private static int TrailingWhitespaceStart(string text, int end)
    {
        var start = end;
        while (start > 0 && char.IsWhiteSpace(text[start - 1]))
        {
            start--;
        }

        return start;
    }
}
