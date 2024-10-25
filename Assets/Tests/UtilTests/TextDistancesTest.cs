using Algorithms;
using NUnit.Framework;


public class TextDistancesTest
{
    [Test]
    public void CalculateButtonPresses_ExactMatchShouldReturnZeroTest()
    {
        string currentText = "The apple";
        string targetText = "The apple";

        int result = TextDistance.CalculateButtonPresses(currentText, targetText);
        Assert.AreEqual(0, result);
    }

    [Test]
    public void CalculateButtonPresses_ExtraCharactersShouldReturnDeleteCountTest()
    {
        string currentText = "Theee apple";
        string targetText = "The apple";

        int result = TextDistance.CalculateButtonPresses(currentText, targetText);
        Assert.AreEqual(14, result);
    }

    [Test]
    public void CalculateButtonPresses_MissingCharactersShouldReturnAddCountTest()
    {
        string currentText = "The ap";
        string targetText = "The apple";

        int result = TextDistance.CalculateButtonPresses(currentText, targetText);
        Assert.AreEqual(3, result);  // Need to add 3 characters: 'ple'
    }

    [Test]
    public void CalculateButtonPresses_ExtraAndMissingCharactersShouldReturnSumOfChangesTest()
    {
        string currentText = "Thee a";
        string targetText = "The apple";

        int result = TextDistance.CalculateButtonPresses(currentText, targetText);
        Assert.AreEqual(9, result);
    }

    [Test]
    public void CalculateButtonPresses_EmptyCurrentTextShouldReturnFullLengthOfTargetTextTest()
    {
        string currentText = "";
        string targetText = "The apple";

        int result = TextDistance.CalculateButtonPresses(currentText, targetText);
        Assert.AreEqual(9, result);  // Need to type the full target text
    }

    [Test]
    public void CalculateButtonPresses_EmptyTargetTextShouldReturnFullLengthOfCurrentTextTest()
    {
        string currentText = "The apple";
        string targetText = "";

        int result = TextDistance.CalculateButtonPresses(currentText, targetText);
        Assert.AreEqual(9, result);  // Need to delete the full current text
    }

    [Test]
    public void CalculateButtonPresses_BothEmptyShouldReturnZeroTest()
    {
        string currentText = "";
        string targetText = "";

        int result = TextDistance.CalculateButtonPresses(currentText, targetText);
        Assert.AreEqual(0, result);  // No changes needed
    }

    [Test]
    public void CalculateButtonPresses_LongTextDifferenceShouldReturnCorrectButtonPressesTest()
    {
        string currentText = "Hello, how are you?";
        string targetText = "Hi! What's up?";

        int result = TextDistance.CalculateButtonPresses(currentText, targetText);
        Assert.AreEqual(31, result);  // Delete 18 characters, add 13 characters, total = 31 presses
    }

    [Test]
    public void GetNumberOfMistakes_NumberOfMistakesMistakeDuringTextTest()
    {
        string currentText = "The sun seys";
        string targetText = "The sun sets";

        int result = TextDistance.GetNumberOfMistakes(currentText, targetText);
        Assert.AreEqual(1, result);
    }

    [Test]
    public void GetNumberOfMistakes_NumberOfMistakesMissingTextTest()
    {
        string currentText = "The sun se";
        string targetText = "The sun sets";

        int result = TextDistance.GetNumberOfMistakes(currentText, targetText);
        Assert.AreEqual(0, result);
    }

    [Test]
    public void GetNumberOfMistakes_NumberOfMistakesMultipleMistakeDuringTextTest()
    {
        string currentText = "The san seys";
        string targetText = "The sun sets";

        int result = TextDistance.GetNumberOfMistakes(currentText, targetText);
        Assert.AreEqual(2, result);
    }

    [Test]
    public void GetNumberOfMistakes_NumberOfMistakesNoMistakeDuringTextTest()
    {
        string currentText = "The sun sets";
        string targetText = "The sun sets";

        int result = TextDistance.GetNumberOfMistakes(currentText, targetText);
        Assert.AreEqual(0, result);
    }

    [Test]
    public void GetNumberOfDeletions_NumberOfDeletionsMistakeInTheMiddleTest()
    {
        string targetText = "Hello from the other side";
        string currentText = "Hello fromm the other side";

        int result = TextDistance.GetNumberOfDeletions(currentText, targetText);
        Assert.AreEqual(16, result);
    }

    [Test]
    public void GetNumberOfDeletions_NumberOfDeletionsEmptyTextTest()
    {
        string targetText = "Hello from the other side";
        string currentText = "";

        int result = TextDistance.GetNumberOfDeletions(currentText, targetText);
        Assert.AreEqual(0, result);
    }

    [Test]
    public void GetNumberOfDeletions_NumberOfDeletionsManyMistakesTest()
    {
        string targetText = "Hello from the other side";
        string currentText = "Hellooo fromm theeee";

        int result = TextDistance.GetNumberOfDeletions(currentText, targetText);
        Assert.AreEqual(15, result);
    }

    [Test]
    public void GetNumberOfDeletions_NumberOfDeletionsLongerTextTest()
    {
        string targetText = "Hello from the other side";
        string currentText = "Hello from the other sideee";

        int result = TextDistance.GetNumberOfDeletions(currentText, targetText);
        Assert.AreEqual(2, result);
    }

    [Test]
    public void GetRewardAfterProofreading_EmptyTextTest()
    {
        string previousBeliefText = "";
        string updatedBeliefText = "";
        string correctText = "She bakes bread";

        float result = TextDistance.GetRewardAfterProofreading(0, previousBeliefText, updatedBeliefText, correctText);
        Assert.AreEqual(0, result);
    }

    [Test]
    public void GetRewardAfterProofreading_EqualTextTest()
    {
        string previousBeliefText = "She bak";
        string updatedBeliefText = "She bak";
        string correctText = "She bakes bread";

        float result = TextDistance.GetRewardAfterProofreading(0, previousBeliefText, updatedBeliefText, correctText);
        Assert.AreEqual(0, result);
    }

    [Test]
    public void GetRewardAfterProofreading_TextCorrectTest()
    {
        string previousBeliefText = "";
        string updatedBeliefText = "She bakes bread";
        string correctText = "She bakes bread";

        float result = TextDistance.GetRewardAfterProofreading(67.5f, previousBeliefText, updatedBeliefText, correctText);
        Assert.AreEqual(0, result);
    }

    [Test]
    public void GetRewardAfterProofreading_CorrectUpdateTest()
    {
        string previousBeliefText = "She bake";
        string updatedBeliefText = "She bakes b";
        string correctText = "She bakes bread";

        //Collected reward for the letters "s b": 38.5 - 22 = 16.5
        float result = TextDistance.GetRewardAfterProofreading(16.5f, previousBeliefText, updatedBeliefText, correctText);
        Assert.AreEqual(0f, result);
    }

    [Test]
    public void GetRewardAfterProofreading_NoCollectedBeliefReward()
    {
        string previousBeliefText = "She bake";
        string updatedBeliefText = "She bakes";
        string correctText = "She bakes bread";

        float result = TextDistance.GetRewardAfterProofreading(0, previousBeliefText, updatedBeliefText, correctText);
        Assert.AreEqual(5, result);
    }

    [Test]
    public void GetRewardAfterProofreading_InstantUpdateMistakeTest()
    {
        //Believed typed text is "s", but was "d", therefore, the agent has collected a erroneous reward of 5. Furthermore a penalty that can
        //equalized by pressing the delete button of 0.5 is added.
        string previousBeliefText = "She bake";
        string updatedBeliefText = "She baked";
        string correctText = "She bakes bread";

        float result = TextDistance.GetRewardAfterProofreading(5, previousBeliefText, updatedBeliefText, correctText);
        Assert.AreEqual(-5.5f, result);
    }

    [Test]
    public void GetRewardAfterProofreading_UpdateMistakeTest()
    {
        string previousBeliefText = "She bake";
        string updatedBeliefText = "She baked b";
        string correctText = "She bakes bread";

        float result = TextDistance.GetRewardAfterProofreading(16.5f, previousBeliefText, updatedBeliefText, correctText);
        Assert.AreEqual(-18f, result);
    }

    [Test]
    public void GetRewardAfterProofreading_UpdateProgressWithMistakeTest()
    {
        string previousBeliefText = "";
        string updatedBeliefText = "She bakes breat";
        string correctText = "She bakes bread";

        float result = TextDistance.GetRewardAfterProofreading(67.5f, previousBeliefText, updatedBeliefText, correctText);
        Assert.AreEqual(-8.5f, result);
    }

    [Test]
    public void GetRewardAfterProofreading_ClickButWithoutChangeImmediate()
    {
        string previousBeliefText = "S";
        string updatedBeliefText = "S";
        string correctText = "She bakes bread";

        float result = TextDistance.GetRewardAfterProofreading(1.5f, previousBeliefText, updatedBeliefText, correctText);
        Assert.AreEqual(-1.5f, result);
    }

    [Test]
    public void GetRewardAfterProofreading_ClickButWithoutChange()
    {
        string previousBeliefText = "S";
        string updatedBeliefText = "She";
        string correctText = "She bakes bread";

        float result = TextDistance.GetRewardAfterProofreading(6f, previousBeliefText, updatedBeliefText, correctText);
        Assert.AreEqual(-2.5f, result);
    }

    [Test]
    public void GetRewardAfterProofreading_LessDeletionsAfterUpdate()
    {
        string previousBeliefText = "She bakes breatt";
        string updatedBeliefText = "She bakes breat";
        string correctText = "She bakes bread";

        float result = TextDistance.GetRewardAfterProofreading(0.5f, previousBeliefText, updatedBeliefText, correctText);
        Assert.AreEqual(0, result);
    }

    [Test]
    public void GetRewardAfterProofreading_UpdateMistakeTooLongTest()
    {
        string previousBeliefText = "";
        string updatedBeliefText = "She bakes breattt";
        string correctText = "She bakes bread";

        float result = TextDistance.GetRewardAfterProofreading(67.5f, previousBeliefText, updatedBeliefText, correctText);
        Assert.AreEqual(-9.5f, result);
    }

    [Test]
    public void GetRewardAfterProofreading_ClicksWithoutChangingTextCountTest()
    {
        string previousBeliefText = "";
        string updatedBeliefText = "kkkk";
        string correctText = "She bakes bread";

        float result = TextDistance.GetRewardAfterProofreading(0, previousBeliefText, updatedBeliefText, correctText);
        Assert.AreEqual(-2f, result);
    }

    [Test]
    public void CalculateLetterFrequencyDistance_EqualTexts()
    {
        string sourceText = "She bakes bread";
        string targetText = "She bakes bread";

        float result = TextDistance.CalculateLetterFrequencyDistance(sourceText, targetText);
        Assert.AreEqual(0, result);
    }

    [Test]
    public void CalculateLetterFrequencyDistance_SimilarTexts()
    {
        string sourceText = "She bakes brea";
        string targetText = "She bakes bread";

        float result = TextDistance.CalculateLetterFrequencyDistance(sourceText, targetText);
        Assert.IsTrue(result < 1, $"Expected distance < {1} but was {result}.");
    }

    [Test]
    public void CalculateLetterFrequencyDistance_DifferentTextsShort()
    {
        string sourceText = "a";
        string targetText = "She bakes bread";

        float result = TextDistance.CalculateLetterFrequencyDistance(sourceText, targetText);
        Assert.IsTrue(result > 10, $"Expected distance > {10f} but was {result}.");
    }

    [Test]
    public void CalculateLetterFrequencyDistance_DifferentChars()
    {
        string sourceText = "a";
        string targetText = "b";

        float result = TextDistance.CalculateLetterFrequencyDistance(sourceText, targetText);
        Assert.AreEqual(1, result);
    }

    [Test]
    public void CalculateLetterFrequencyDistance_DifferentTexts()
    {
        string sourceText = "a";
        string targetText = "ab";

        float result = TextDistance.CalculateLetterFrequencyDistance(sourceText, targetText);
        Assert.AreEqual(1, result);
    }

    [Test]
    public void CalculateLetterFrequencyDistance_DifferentTextsLong()
    {
        string sourceText = "Hubert was here";
        string targetText = "She bakes bread";

        float result = TextDistance.CalculateLetterFrequencyDistance(sourceText, targetText);
        Assert.IsTrue(result < 5, $"Expected distance < {5f} but was {result}.");
    }

    [Test]
    public void CalculateLetterFrequencyDistance_DifferentTextsRubbish()
    {
        string sourceText = "xycpllllmlk";
        string targetText = "She bakes bread";

        float result = TextDistance.CalculateLetterFrequencyDistance(sourceText, targetText);
        Assert.IsTrue(result > 10, $"Expected distance > {10f} but was {result}.");
    }

    [Test]
    public void CalculateLetterFrequencyDistance_EmptyText()
    {
        string sourceText = "";
        string targetText = "She bakes bread";

        float result = TextDistance.CalculateLetterFrequencyDistance(sourceText, targetText);
        Assert.AreEqual(15, result);
    }

    [Test]
    public void CalculateLetterFrequencyDistance_DifferentLetterOrdering()
    {
        string sourceText = "daerb sekab ehs";
        string targetText = "She bakes bread";

        float result = TextDistance.CalculateLetterFrequencyDistance(sourceText, targetText);
        Assert.AreEqual(0, result);
    }

    [Test]
    public void CalculateLetterFrequencyDistance_SameFrequencyDifferentLength()
    {
        string sourceText = "a";
        string targetText = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

        float result = TextDistance.CalculateLetterFrequencyDistance(sourceText, targetText);
        Assert.AreEqual(0, result);
    }

    [Test]
    public void CalculateLetterFrequencyDistance_DifferentLength()
    {
        string sourceText = "b";
        string targetText = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

        float result = TextDistance.CalculateLetterFrequencyDistance(sourceText, targetText);
        Assert.AreEqual(targetText.Length, result);
    }

    [Test]
    public void CalculateLetterFrequencyDistance_SourceLongerThanTarget()
    {
        string sourceText = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        string targetText = "b";

        float result = TextDistance.CalculateLetterFrequencyDistance(sourceText, targetText);
        Assert.AreEqual(1, result);
    }
}