using Microsoft.AspNetCore.Mvc;
using MotoTrack.Api.DTOs;

namespace MotoTrack.Api.Utils;

public static class Hateoas
{
    public static void AddPaginationLinks<T>(PagedResult<T> paged, IUrlHelper url, string routeName, object? routeValues = null)
    {
        var values = new Dictionary<string, object?>();
        if (routeValues != null)
        {
            foreach (var prop in routeValues.GetType().GetProperties())
                values[prop.Name] = prop.GetValue(routeValues);
        }

        // self
        values["page"] = paged.Page;
        values["size"] = paged.Size;
        paged.Links.Add(new Link("self", url.Link(routeName, values)!, "GET"));

        // next
        if (paged.Page < paged.TotalPages)
        {
            values["page"] = paged.Page + 1;
            paged.Links.Add(new Link("next", url.Link(routeName, values)!, "GET"));
        }

        // prev
        if (paged.Page > 1)
        {
            values["page"] = paged.Page - 1;
            paged.Links.Add(new Link("prev", url.Link(routeName, values)!, "GET"));
        }
    }
}