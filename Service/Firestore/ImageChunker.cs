using System;
using System.Collections.Generic;

namespace ClipboardWizard.Service.Firestore
{
    /// <summary>
    /// Splits/reassembles raw image bytes for Firestore's 1 MiB per-document cap. Chunk
    /// boundaries are on raw bytes (each chunk base64-encoded independently by the caller), so
    /// reassembly is a plain concatenation of decoded chunks in order - no boundary bookkeeping
    /// needed beyond ordering.
    /// </summary>
    public static class ImageChunker
    {
        /// <summary>
        /// Headroom under Firestore's 1 MiB hard per-document cap, after base64's ~33% inflation
        /// plus the snippet document's other fields. An image at or under this size is stored
        /// inline on the snippet document itself; anything larger is split into ChunkThresholdBytes-sized
        /// pieces, one per document in the snippet's imageChunks subcollection.
        /// </summary>
        public const int ChunkThresholdBytes = 600_000;

        public static IReadOnlyList<byte[]> Split(byte[] imageData, int chunkSizeBytes)
        {
            if (imageData == null)
            {
                throw new ArgumentNullException(nameof(imageData));
            }

            if (chunkSizeBytes <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(chunkSizeBytes));
            }

            if (imageData.Length == 0)
            {
                return Array.Empty<byte[]>();
            }

            int chunkCount = (imageData.Length + chunkSizeBytes - 1) / chunkSizeBytes;
            List<byte[]> chunks = new(chunkCount);

            for (int offset = 0; offset < imageData.Length; offset += chunkSizeBytes)
            {
                int length = Math.Min(chunkSizeBytes, imageData.Length - offset);
                byte[] chunk = new byte[length];
                Array.Copy(imageData, offset, chunk, 0, length);
                chunks.Add(chunk);
            }

            return chunks;
        }

        public static byte[] Reassemble(IReadOnlyList<byte[]> orderedChunks)
        {
            if (orderedChunks == null)
            {
                throw new ArgumentNullException(nameof(orderedChunks));
            }

            int totalLength = 0;
            foreach (byte[] chunk in orderedChunks)
            {
                totalLength += chunk.Length;
            }

            byte[] result = new byte[totalLength];
            int offset = 0;
            foreach (byte[] chunk in orderedChunks)
            {
                Array.Copy(chunk, 0, result, offset, chunk.Length);
                offset += chunk.Length;
            }

            return result;
        }
    }
}
