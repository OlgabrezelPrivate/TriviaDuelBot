using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;
using Newtonsoft.Json;
using System.Linq;

namespace TriviaDuelBot.TriviaDuel
{
    public static class QuestionAPI
    {
        #region Categories
        private static List<TriviaCategory> CachedCategories = null;
        private static DateTime CategoryCacheTime = DateTime.MinValue;

        /// <summary>
        /// Gets all categories from the question API. The results of this method are cached
        /// for <see cref="Constants.CategoryCacheTime"/> minutes.
        /// </summary>
        /// <returns>All categories of the question API</returns>
        public static async Task<List<TriviaCategory>> GetCategories()
        {
            if (CachedCategories == null || DateTime.UtcNow - CategoryCacheTime > TimeSpan.FromMinutes(Constants.CategoryCacheTime))
            {
                CachedCategories = await RetrieveCategories();
                CategoryCacheTime = DateTime.UtcNow;
            }

            return CachedCategories;
        }

        const string CategoriesURL = "https://opentdb.com/api_category.php";
        private static async Task<List<TriviaCategory>> RetrieveCategories()
        {
            HttpResponseMessage res;
            using (var client = new HttpClient())
                res = await client.GetAsync(CategoriesURL);

            if (!res.IsSuccessStatusCode) return null;

            var str = await res.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<TriviaCategoryResult>(str);

            foreach (var c in result.TriviaCategories)
            {
                c.Name = WebUtility.HtmlDecode(c.Name);
            }
            return result.TriviaCategories;
        }
        #endregion

        #region Questions
        const string QuestionsURL = "https://opentdb.com/api.php?amount=3&category={0}&type=multiple";

        /// <summary>
        /// Gets 3 questions in the category with ID <paramref name="categoryId"/>.
        /// This method always calls the question API to get the questions.
        /// </summary>
        /// <param name="categoryId">The category ID to get questions in</param>
        /// <returns>3 questions in the category with ID <paramref name="categoryId"/>.</returns>
        public static async Task<List<TriviaQuestion>> GetQuestions(int categoryId)
        {
            string url = string.Format(QuestionsURL, categoryId);

            HttpResponseMessage res;
            using (var client = new HttpClient())
                res = await client.GetAsync(url);

            if (!res.IsSuccessStatusCode) return null;

            var str = await res.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<TriviaQuestionResult>(str);

            if (result.ResponseCode != TriviaResponseCode.Success) return null;
            foreach (var q in result.Results)
            {
                q.Category = WebUtility.HtmlDecode(q.Category);
                q.Question = WebUtility.HtmlDecode(q.Question);
                q.CorrectAnswer = WebUtility.HtmlDecode(q.CorrectAnswer);
                for (int i = 0; i < q.IncorrectAnswers.Count; i++)
                    q.IncorrectAnswers[i] = WebUtility.HtmlDecode(q.IncorrectAnswers[i]);
            }
            return result.Results;
        }
        #endregion
    }

    #region Categories
    [JsonObject]
    public class TriviaCategoryResult
    {
        [JsonProperty("trivia_categories")]
        public List<TriviaCategory> TriviaCategories { get; set; }
    }

    [JsonObject]
    public class TriviaCategory
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }
    #endregion

    #region Questions
    public enum TriviaResponseCode
    {
        Success = 0,
        NoResults = 1,
        InvalidParameter = 2,
        TokenNotFound = 3,
        TokenEmpty = 4,
    }

    [JsonObject]
    public class TriviaQuestionResult
    {
        [JsonProperty("response_code")]
        public TriviaResponseCode ResponseCode { get; set; }

        [JsonProperty("results")]
        public List<TriviaQuestion> Results { get; set; }
    }

    public class TriviaQuestion
    {
        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("difficulty")]
        public string Difficulty { get; set; }

        [JsonProperty("question")]
        public string Question { get; set; }

        [JsonProperty("correct_answer")]
        public string CorrectAnswer { get; set; }

        [JsonProperty("incorrect_answers")]
        public List<string> IncorrectAnswers { get; set; }
    }
    #endregion
}
