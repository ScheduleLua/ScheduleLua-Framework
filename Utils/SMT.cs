using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace StringMatchingTools
{
    public static class SMT
    {
        private static readonly HashSet<string> CommonWords = new HashSet<string>
        {
            "the", "and", "a", "to", "in", "that", "it", "with", "as", "for", "was", "on", "are", "be", "by", "at",
            "an", "this", "who", "which", "or", "but", "not", "is", "error", "can", "were", "been", "being", "one",
            "can't", "do", "of", "if", "you", "they", "we", "all", "my", "your", "he", "she", "there", "some",
            "also", "what", "just", "so", "only", "like", "well", "will", "much", "more", "most", "no", "yes", "our"
        };

        /// <summary>
        /// Extracts keywords from an input string, optionally using a list of preferred words.
        /// </summary>
        /// <param name="input">The input string to extract keywords from.</param>
        /// <param name="amt">Maximum number of keywords to extract.</param>
        /// <param name="path">Optional path to a file containing preferred words.</param>
        /// <returns>A string containing the extracted keywords.</returns>
        public static string ExtractKeywords(this string input, int amt, string path = null)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            List<string> preferredWords = new List<string>();
            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    preferredWords = File.ReadAllLines(path).ToList();
                }
                catch (IOException ex)
                {
                    Debug.LogError($"Failed to read preferred words file: {ex.Message}");
                }
            }

            var words = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(w => !string.IsNullOrWhiteSpace(w) && !CommonWords.Contains(w.ToLowerInvariant()));

            if (preferredWords.Count > 0)
            {
                words = words
                    .Where(w => preferredWords.Contains(w.ToLowerInvariant()))
                    .Concat(words.Where(w => !preferredWords.Contains(w.ToLowerInvariant())));
            }

            words = words.Take(amt);

            return string.Join(" ", words);
        }

        /// <summary>
        /// Calculates the Levenshtein distance between two strings.
        /// </summary>
        /// <param name="source1">First string.</param>
        /// <param name="source2">Second string.</param>
        /// <param name="maxDistance">Optional maximum distance threshold for early termination.</param>
        /// <returns>The Levenshtein distance between the strings.</returns>
        private static int Calculate(string source1, string source2, int maxDistance = int.MaxValue)
        {
            // Quick check for common cases
            if (string.IsNullOrEmpty(source1)) return source2?.Length ?? 0;
            if (string.IsNullOrEmpty(source2)) return source1.Length;
            if (source1 == source2) return 0;

            // Ensure source1 is the shorter string for memory optimization
            if (source1.Length > source2.Length)
            {
                var temp = source1;
                source1 = source2;
                source2 = temp;
            }

            int source1Length = source1.Length;
            int source2Length = source2.Length;
            
            // If the difference in string lengths exceeds maxDistance,
            // we can return early since the Levenshtein distance can't be less
            if (source2Length - source1Length > maxDistance) return maxDistance + 1;

            // Memory optimization: we only need two rows of the matrix
            int[] previousRow = new int[source1Length + 1];
            int[] currentRow = new int[source1Length + 1];

            // Initialize the first row
            for (int i = 0; i <= source1Length; i++)
                previousRow[i] = i;

            // Fill in the matrix
            for (int j = 1; j <= source2Length; j++)
            {
                currentRow[0] = j;
                
                // Track if we've found a row where all values exceed maxDistance
                bool allExceedThreshold = true;

                for (int i = 1; i <= source1Length; i++)
                {
                    int cost = (source1[i - 1] == source2[j - 1]) ? 0 : 1;

                    currentRow[i] = Math.Min(
                        Math.Min(previousRow[i] + 1, currentRow[i - 1] + 1),
                        previousRow[i - 1] + cost);
                    
                    if (currentRow[i] <= maxDistance)
                        allExceedThreshold = false;
                }
                
                // Early termination if all values in this row exceed maxDistance
                if (allExceedThreshold && maxDistance < int.MaxValue)
                    return maxDistance + 1;

                // Swap rows for next iteration
                var temp = previousRow;
                previousRow = currentRow;
                currentRow = temp;
            }

            return previousRow[source1Length];
        }

        /// <summary>
        /// Preprocesses input text by converting to lowercase, removing punctuation and common words.
        /// </summary>
        /// <param name="input">The input string to preprocess.</param>
        /// <returns>Preprocessed string.</returns>
        private static string Preprocess(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            
            input = input.ToLowerInvariant();
            input = new string(input.Where(c => !char.IsPunctuation(c)).ToArray());

            var words = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(w => !string.IsNullOrWhiteSpace(w) && !CommonWords.Contains(w));

            return string.Join(" ", words);
        }

        /// <summary>
        /// Calculates the similarity between two strings.
        /// </summary>
        /// <param name="uInput">First input string.</param>
        /// <param name="uInput2">Second input string.</param>
        /// <param name="preProcess">Whether to preprocess the inputs.</param>
        /// <param name="maxDistanceFactor">Maximum distance as a fraction of the longer string length (0.0-1.0).</param>
        /// <returns>Similarity score between 0.0 and 1.0.</returns>
        public static double Check(string uInput, string uInput2, bool preProcess, double maxDistanceFactor = 1.0)
        {
            if (string.IsNullOrEmpty(uInput) && string.IsNullOrEmpty(uInput2)) return 1.0;
            if (string.IsNullOrEmpty(uInput) || string.IsNullOrEmpty(uInput2)) return 0.0;
            if (uInput == uInput2) return 1.0;

            if (preProcess)
            {
                uInput = Preprocess(uInput);
                uInput2 = Preprocess(uInput2);
                
                // Check again after preprocessing
                if (string.IsNullOrEmpty(uInput) && string.IsNullOrEmpty(uInput2)) return 1.0;
                if (string.IsNullOrEmpty(uInput) || string.IsNullOrEmpty(uInput2)) return 0.0;
                if (uInput == uInput2) return 1.0;
            }

            int maxLength = Math.Max(uInput.Length, uInput2.Length);
            int maxDistance = maxDistanceFactor < 1.0 ? (int)(maxLength * maxDistanceFactor) : maxLength;

            int distance = Calculate(uInput, uInput2, maxDistance);
            
            // If distance exceeds threshold, it means we hit early termination
            if (distance > maxDistance) 
                return 0.0;
                
            double similarity = 1.0 - (double)distance / maxLength;
            return similarity;
        }
    }
}