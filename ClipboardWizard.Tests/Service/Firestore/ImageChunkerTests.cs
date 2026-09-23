using ClipboardWizard.Service.Firestore;

namespace ClipboardWizard.Tests.Service.Firestore
{
    public class ImageChunkerTests
    {
        [Fact]
        public void Split_DataSmallerThanChunkSize_ReturnsSingleChunk()
        {
            byte[] data = [1, 2, 3];

            IReadOnlyList<byte[]> chunks = ImageChunker.Split(data, chunkSizeBytes: 10);

            Assert.Single(chunks);
            Assert.Equal(data, chunks[0]);
        }

        [Fact]
        public void Split_DataExactMultipleOfChunkSize_DoesNotProduceTrailingEmptyChunk()
        {
            byte[] data = new byte[20];

            IReadOnlyList<byte[]> chunks = ImageChunker.Split(data, chunkSizeBytes: 10);

            Assert.Equal(2, chunks.Count);
            Assert.All(chunks, chunk => Assert.Equal(10, chunk.Length));
        }

        [Fact]
        public void Split_DataOneByteOverChunkBoundary_ProducesSmallFinalChunk()
        {
            byte[] data = new byte[21];

            IReadOnlyList<byte[]> chunks = ImageChunker.Split(data, chunkSizeBytes: 10);

            Assert.Equal(3, chunks.Count);
            Assert.Equal(10, chunks[0].Length);
            Assert.Equal(10, chunks[1].Length);
            Assert.Single(chunks[2]);
        }

        [Fact]
        public void Split_EmptyData_ReturnsNoChunks()
        {
            IReadOnlyList<byte[]> chunks = ImageChunker.Split([], chunkSizeBytes: 10);

            Assert.Empty(chunks);
        }

        [Fact]
        public void SplitThenReassemble_RoundTripsOriginalBytes()
        {
            byte[] data = new byte[ImageChunker.ChunkThresholdBytes * 2 + 137];
            new Random(42).NextBytes(data);

            IReadOnlyList<byte[]> chunks = ImageChunker.Split(data, ImageChunker.ChunkThresholdBytes);
            byte[] reassembled = ImageChunker.Reassemble(chunks);

            Assert.Equal(data, reassembled);
        }

        [Fact]
        public void Reassemble_PreservesChunkOrder()
        {
            byte[][] chunks = [[1, 2], [3, 4], [5]];

            byte[] reassembled = ImageChunker.Reassemble(chunks);

            Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, reassembled);
        }
    }
}
