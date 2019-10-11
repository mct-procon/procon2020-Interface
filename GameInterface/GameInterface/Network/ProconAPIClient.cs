using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using MCTProcon30Protocol.Json;
using System.Threading.Tasks;
using System.Net.Http;

namespace GameInterface.Network
{
    public class ProconAPIClient
    {
        public static ProconAPIClient Instance { get; private set; }
        public static NetworkInformation Information { get; set; }


        private HttpClient hc { get; set; }
        public Match[] Matches { get; private set; }

        public ErrorResponse LastError { get; private set; }
        public Field FieldState { get; private set; }
        public Match MatchData { get; private set; }

        static ProconAPIClient()
        {
            Information = new NetworkInformation();
            Instance = new ProconAPIClient();
        }

        private ProconAPIClient()
        {
            hc = new HttpClient();
        }

        private void PrepareHeader()
        {
            hc.DefaultRequestHeaders.Clear();
            if (!string.IsNullOrEmpty(Information.AuthenticationID))
                hc.DefaultRequestHeaders.Add("Authorization", Information.AuthenticationID);
        }

        public void SetMatchData(Match match) => MatchData = match;

        public async Task<Match[]> GetMatches()
        {
            PrepareHeader();
            var response = await hc.GetAsync(Information.URLStarts + "matches");
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var result = JsonConvert.DeserializeObject<Match[]>(await response.Content.ReadAsStringAsync());
                Matches = result;
                return result;
            }
            else
            {
                var result = JsonConvert.DeserializeObject<ErrorResponse>(await response.Content.ReadAsStringAsync());
                throw new Exception("無効なトークンです");
            }
        }

        public async Task<bool> GetState(int id)
        {
            PrepareHeader();
            var response = await hc.GetAsync(Information.URLStarts + "matches/" + id);
            if(response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                FieldState = JsonConvert.DeserializeObject<Field>(await response.Content.ReadAsStringAsync());
                return true;
            }
            else
            {
                LastError = JsonConvert.DeserializeObject<ErrorResponse>(await response.Content.ReadAsStringAsync());
                return false;
            }
        }
    }
}
