namespace BankingAPI.Models.DTOs{
    public class PagedResponse<T>{
        public List<T> Data {get; set;} = new();
        public int Page {get; set;}
        public int PageSize {get; set;}
        public int TotalCount {get; set;}
        public int TotalPages => (int)Math.Ceiling((double)TotalCount/PageSize);
        public bool HasNext => Page< TotalPages;
        public bool HasPrevious => Page > 1;
    }
}