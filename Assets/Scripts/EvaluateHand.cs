using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

public class EvaluateHand : MonoBehaviour
{
	public TextAsset commonWordList;
	public TextAsset comprehensiveWordList;
	Dictionary<char, int> letterScores;
	public int[] wordLengthBonuses;
	
	public static EvaluateHand instance;
	
	void Awake()
    {
		letterScores = new Dictionary<char, int>
		{
			{'A', 1}, {'B', 3}, {'C', 3}, {'D', 2},
			{'E', 1}, {'F', 4}, {'G', 2}, {'H', 4},
			{'I', 1}, {'J', 8}, {'K', 5}, {'L', 1},
			{'M', 3}, {'N', 1}, {'O', 1}, {'P', 3},
			{'Q', 10},{'R', 1}, {'S', 1}, {'T', 1},
			{'U', 1}, {'V', 4}, {'W', 4}, {'X', 8},
			{'Y', 4}, {'Z', 10}
		};
		instance = this;
    }
	
	HashSet<string>[] wordLists;
	
	// HashSet<string> commonWords;
	// HashSet<string> comprehensiveWords;
	
	void LoadWordList()
	{
		wordLists = new HashSet<string>[2];
		string[] commonWordArray = commonWordList.text.Split('\n');
		wordLists[0] = new HashSet<string>(commonWordArray.Select(word => word.Trim().ToLower()));
		// print("Loaded " + commonWords.Count + " words into the common dictionary");
		string[] comprehensiveWordArray = comprehensiveWordList.text.Split('\n');
		wordLists[1] = new HashSet<string>(comprehensiveWordArray.Select(word => word.Trim().ToLower()));
		// print("Loaded " + comprehensiveWords.Count + " words into the common dictionary");
	}
	
	void Start()
	{
/* 		if (letterScores == null)
		{
			Debug.LogError("letterScores dictionary is not initialized!");
		}
		else
		{
			Debug.Log("letterScores dictionary is initialized with " + letterScores.Count + " entries.");
		}  */
		
		LoadWordList();
		
		/* string letters = "eaoknhg";
		string bestWord = FindBestWord(letters);
		print("Best word from letters " + letters + " is " + bestWord); */
	}
	
/* 	public EvaluateHand(IEnumerable<string> wordList)
    {
        validWords = new HashSet<string>(wordList);
    }
     */
    public string FindBestWord(string letters, int whichHashSet)
    {
		HashSet<string> validWords = wordLists[whichHashSet];
		letters = letters.ToLower();
        List<string> allPossibleWords = GeneratePossibleWords(letters);
        string bestWord = "";
        int bestScore = 0;

        foreach (var word in allPossibleWords)
        {
            if (validWords.Contains(word))
            {
                int wordScore = CalculateWordScore(word);
                if (wordScore > bestScore)
                {
                    bestScore = wordScore;
                    bestWord = word;
                }
            }
        }

        return bestWord;
    }
	
	private List<string> GeneratePossibleWords(string letters)
    {
        var result = new List<string>();
        var chars = letters.ToCharArray();

        // Generate all subsets of letters
        for (int i = 1; i <= chars.Length; i++)
        {
            var subsets = GetSubsets(chars, i);

            // Generate all permutations of each subset
            foreach (var subset in subsets)
            {
                var permutations = GetPermutations(subset);
                result.AddRange(permutations);
            }
        }

        return result.Distinct().ToList();
    }
	
	private IEnumerable<string> GetSubsets(char[] letters, int subsetLength)
    {
        if (subsetLength == 1)
        {
            return letters.Select(l => l.ToString());
        }

        var subsets = new List<string>();

        for (int i = 0; i < letters.Length; i++)
        {
            var remainingLetters = letters.Skip(i + 1).ToArray();
            foreach (var subset in GetSubsets(remainingLetters, subsetLength - 1))
            {
                subsets.Add(letters[i] + subset);
            }
        }

        return subsets;
    }
	
	private IEnumerable<string> GetPermutations(string str)
    {
        if (str.Length == 1)
            return new List<string> { str };

        var permutations = new List<string>();

        for (int i = 0; i < str.Length; i++)
        {
            var remainingLetters = str.Remove(i, 1);
            foreach (var permutation in GetPermutations(remainingLetters))
            {
                permutations.Add(str[i] + permutation);
            }
        }

        return permutations;
    }
	
	public int CalculateWordScore(string word)
    {
		if(word.Length > 7 || word.Length <= 0)
		{
			return 0;
		}
		int score = 0;
        foreach (var letter in word)
        {
			if (char.IsLetter(letter))
			{
				char upperLetter = char.ToUpper(letter);
				if (letterScores.ContainsKey(upperLetter))
				{
					score += letterScores[upperLetter];
				}
				else
				{
					Debug.LogWarning($"Letter '{letter}' in word '{word}' is not valid or has no score.");
				}
			}
			else
			{
				Debug.LogWarning($"Letter '{letter}' in word '{word}' is not a letter.");
			}
        }
/* 		if(word.Length > 7 || word.Length <= 0)
		{
			Debug.Log($"<color=green>[Silver]</color> Looking at word: {word} with length {word.Length}");
		} */
		
		score += wordLengthBonuses[word.Length - 1];
        return score;
    }
}
