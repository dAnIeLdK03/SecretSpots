using SecretSpots.Features.Assistant;

namespace SecretSpots.Features.Tests.Assistant;

public class SpotMarkerStreamFilterTests
{
    private static string Run(string text, int chunkSize)
    {
        var filter = new SpotMarkerStreamFilter();
        var output = string.Concat(text.Chunk(chunkSize).Select(chars => filter.Push(new string(chars))));
        return output + filter.Flush();
    }

    [Theory]
    [InlineData("Try Hidden falls [spot:3fa85f64-5717-4562-b3fc-2c963f66afa6], it's quiet.", "Try Hidden falls, it's quiet.")]
    [InlineData("First, [spot:3fa85f64] - the falls.", "First, - the falls.")]
    [InlineData("Two: [spot:aa11bb22] and [spot:cc33dd44].", "Two: and.")]
    [InlineData("A list [1] and [spot is a word].", "A list [1] and [spot is a word].")]
    [InlineData("Ends with an unfinished [spot:ab12", "Ends with an unfinished [spot:ab12")]
    [InlineData("No markers at all.  ", "No markers at all.  ")]
    public void Markers_are_removed_however_the_text_is_split(string text, string expected)
    {
        for (var chunkSize = 1; chunkSize <= text.Length; chunkSize++)
        {
            Assert.Equal(expected, Run(text, chunkSize));
        }
    }
}
