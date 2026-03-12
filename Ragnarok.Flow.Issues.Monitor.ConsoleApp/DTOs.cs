using Newtonsoft.Json;

namespace Ragnarok.Flow.Issues.Monitor.ConsoleApp
{
    public record WiqlResponse(
        [property: JsonProperty("workItems")] List<WorkItemRef> WorkItems
    );

    public record WorkItemRef(
        [property: JsonProperty("id")] int Id,
        [property: JsonProperty("url")] string? Url = null,
        [property: JsonProperty("title")] string? Title = null
    );

    public record CommentsResponse(
        [property: JsonProperty("totalCount")] int TotalCount,
        [property: JsonProperty("count")] int Count,
        [property: JsonProperty("comments")] List<Comment> Comments
    );

    public record Comment(
        [property: JsonProperty("workItemId")] int WorkItemId,
        [property: JsonProperty("id")] int Id,
        [property: JsonProperty("version")] int Version,
        [property: JsonProperty("text")] string? Text,
        [property: JsonProperty("renderedText")] string? RenderedText,
        [property: JsonProperty("createdBy")] Identity? CreatedBy,
        [property: JsonProperty("createdDate")] DateTime CreatedDate,
        [property: JsonProperty("modifiedBy")] Identity? ModifiedBy,
        [property: JsonProperty("modifiedDate")] DateTime? ModifiedDate,
        [property: JsonProperty("format")] string? Format,
        [property: JsonProperty("url")] string? Url
    );

    public record Identity(
        [property: JsonProperty("displayName")] string? DisplayName,
        [property: JsonProperty("uniqueName")] string? UniqueName,
        [property: JsonProperty("id")] string? Id,
        [property: JsonProperty("imageUrl")] string? ImageUrl,
        [property: JsonProperty("url")] string? Url,
        [property: JsonProperty("descriptor")] string? Descriptor
    );

    public record WorkItemFields(
        [property: JsonProperty("System.Title")] string? Title
    );

    public record WorkItemDetailsResponse(
        [property: JsonProperty("id")] int Id,
        [property: JsonProperty("fields")] WorkItemFields Fields
    );

}
