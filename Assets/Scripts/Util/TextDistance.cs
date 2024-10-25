using System;
using System.Collections.Generic;
using System.Linq;

namespace Algorithms
{
    public static class TextDistance
    {
        /// <summary>
        ///     Calculate the difference between 2 strings using the Levenshtein distance algorithm
        /// </summary>
        /// <param name="source1">First string</param>
        /// <param name="source2">Second string</param>
        /// <returns></returns>
        public static int CalculateLevenshtein(string source1, string source2) //O(n*m)
        {
            var source1Length = source1.Length;
            var source2Length = source2.Length;

            var matrix = new int[source1Length + 1, source2Length + 1];

            // First calculation, if one entry is empty return full length
            if (source1Length == 0)
                return source2Length;

            if (source2Length == 0)
                return source1Length;

            // Initialization of matrix with row size source1Length and columns size source2Length
            for (var i = 0; i <= source1Length; matrix[i, 0] = i++) { }
            for (var j = 0; j <= source2Length; matrix[0, j] = j++) { }

            // Calculate rows and collumns distances
            for (var i = 1; i <= source1Length; i++)
            {
                for (var j = 1; j <= source2Length; j++)
                {
                    var cost = (source2[j - 1] == source1[i - 1]) ? 0 : 1;

                    matrix[i, j] = Math.Min(
                        Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                        matrix[i - 1, j - 1] + cost);
                }
            }
            // return result
            return matrix[source1Length, source2Length];
        }

        public static int CalculateDamerauLevenshtein(string s1, string s2)
        {
            // Create a table to store the results of
            // subproblems
            int[,] dp = new int[s1.Length + 1, s2.Length + 1];
            // Initialize the table
            for (int i = 0; i <= s1.Length; i++)
            {
                dp[i, 0] = i;
            }
            for (int j = 0; j <= s2.Length; j++)
            {
                dp[0, j] = j;
            }

            // Populate the table using dynamic programming
            for (int i = 1; i <= s1.Length; i++)
            {
                for (int j = 1; j <= s2.Length; j++)
                {
                    if (s1[i - 1] == s2[j - 1])
                    {
                        dp[i, j] = dp[i - 1, j - 1];
                    }
                    else
                    {
                        dp[i, j] = 1 + Math.Min(dp[i - 1, j], Math.Min(dp[i, j - 1], dp[i - 1, j - 1]));
                    }
                }
            }

            // Return the edit distance
            return dp[s1.Length, s2.Length];
        }

        /// <summary>
        /// Mistake is defined as a character that is present in the currentText but not in the targetText. Missing characters at the end of the 
        /// currentText are not considered mistakes.
        /// </summary>
        /// <param name="s1"></param>
        /// <param name="s2"></param>
        /// <returns></returns>
        public static int GetNumberOfMistakes(string currentText, string targetText)
        {
            int mistakes = 0;

            for (int i = 0; i < currentText.Length; i++)
            {
                if (i < targetText.Length)
                {
                    if (currentText[i] != targetText[i])
                    {
                        mistakes++;
                    }
                }
                else
                {
                    mistakes++;
                }
            }

            return mistakes;
        }

        public static int GetNumberOfDeletions(string currentText, string targetText)
        {
            for (int i = 0; i < currentText.Length; i++)
            {
                if (i < targetText.Length)
                {
                    if (currentText[i] != targetText[i])
                    {
                        return currentText.Length - i;
                    }
                }
                else
                {
                    return currentText.Length - targetText.Length;
                }
            }

            return currentText.Length > targetText.Length ? currentText.Length - targetText.Length : 0;
        }

        public static float GetRewardAfterProofreading(float collectedBeliefReward, string lastProofedText, string newText, string targetText)
        {
            if(newText == lastProofedText && collectedBeliefReward > 0)
            {
                return -collectedBeliefReward;
            }
            else if (newText == lastProofedText)
            {
                return 0;
            }

            int numberOfCorrectLettersLastProofedText = GetNumberOfCorrectLetters(lastProofedText, targetText);
            float rewardLastProofedText = GetBeliefRewardForNumberOfLetters(numberOfCorrectLettersLastProofedText);

            int numberOfCorrectLettersNewText = GetNumberOfCorrectLetters(newText, targetText);
            float rewardNewText = GetBeliefRewardForNumberOfLetters(numberOfCorrectLettersNewText);

            float proofreadingReward = rewardNewText - rewardLastProofedText;

            int numberOfDeletions = GetNumberOfDeletions(newText, targetText) > GetNumberOfDeletions(lastProofedText, targetText) ? GetNumberOfDeletions(newText, targetText) : -GetNumberOfDeletions(newText, targetText);
            proofreadingReward = proofreadingReward - 0.5f * numberOfDeletions;

            return proofreadingReward - collectedBeliefReward;
        }

        public static int CalculateButtonPresses(string currentText, string targetText)
        {
            // Find the common prefix between the two texts
            int commonLength = 0;
            while (commonLength < currentText.Length
                && commonLength < targetText.Length
                && currentText[commonLength] == targetText[commonLength])
            {
                commonLength++;
            }

            // Characters to delete from currentText
            int charactersToDelete = currentText.Length - commonLength;

            // Characters to add to match targetText
            int charactersToAdd = targetText.Length - commonLength;

            // Total button presses: deletions + additions
            return charactersToDelete + charactersToAdd;
        }

        public static float CalculateLetterFrequencyDistance(string sourceText, string targetText)
        {
            // Handle case where the text has no letters
            if (sourceText.Length == 0)
            {
                return targetText.Length;
            }

            Dictionary<char, float> sourceTextFrequency = GetCharFrequency(sourceText);
            Dictionary<char, float> targetTextFrequency = GetCharFrequency(targetText);
            float frequencySum = 0;

            // Calculate the deviation score by comparing to typical English letter frequencies
            float frequencyScore = 0.0f;
            foreach (var letter in targetTextFrequency.Keys)
            {
                float expectedFrequency = (targetTextFrequency[letter] / targetText.Length);
                float actualFrequency = sourceTextFrequency.ContainsKey(letter) ? (sourceTextFrequency[letter] / sourceText.Length) : 0.0f;
                frequencySum += expectedFrequency + actualFrequency;
                frequencyScore += Math.Abs(expectedFrequency - actualFrequency);
            }

            float normalizedFrequency = frequencyScore / frequencySum;

            return normalizedFrequency * targetText.Length; // Return distance, max is expectedLength
        }

        private static Dictionary<char, float> GetCharFrequency(string text)
        {
            var letters = text.ToLower().ToList();

            Dictionary<char, float> letterCounts = new();
            foreach (var c in letters)
            {
                if (letterCounts.ContainsKey(c))
                    letterCounts[c]++;
                else
                    letterCounts[c] = 1;
            }

            return letterCounts;
        }

        private static int GetNumberOfCorrectLetters(string currentText, string targetText)
        {
            int correctLetters = 0;

            for (int i = 0; i < currentText.Length; i++)
            {
                if (i < targetText.Length)
                {
                    if (currentText[i] == targetText[i])
                    {
                        correctLetters++;
                    }
                    else
                    {
                       break;
                    }
                }
            }

            return correctLetters;
        }

        private static float GetBeliefRewardForNumberOfLetters(int n)
        {
            return n / (float)2 * (n / (float)2 + 1.5f); //partial sum for a_n = a_{n-1} + 0.5
        }
    }
}