using System.Security.Claims;
using Api.Models;
using Api.Services;
using Api.Utils;
using AutoMapper;
using Domain.Abstractions.Services;
using Domain.Models.Dtos;
using Domain.Models.Dtos.Property;

namespace Api.Endpoints;

public static class PropertyEndpointsExt
{
    public static IEndpointRouteBuilder MapProperties(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/properties");

        group.MapPost("/create", async (PropertyCreate propertyCreate, IPropertyService propertyService,
            HttpResponseCreator httpCreator, HttpContext context, IMapper mapper) =>
        {
            var userIdClaim = context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            var success = int.TryParse(userIdClaim?.Value, out int userId);
            if (!success)
            {
                return Results.Forbid();
            }
            var propertyCreateRequest = mapper.Map<PropertyCreateRequest>(propertyCreate);
            var propertyIdResult = await propertyService.CreatePropertyAsync(propertyCreateRequest, userId);
            return httpCreator.CreateResponse(propertyIdResult);
        })
        .RequireAuthorization();

        group.MapGet("/search", async (PropertySearchRequest propertySearchRequest, IPropertyService propertyService,
            HttpResponseCreator httpCreator, HttpContext context) =>
        {
            var userIdClaim = context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            var success = int.TryParse(userIdClaim?.Value, out int userId);
            var propertySearchResponse =
                await propertyService.SearchPropertiesAsync(propertySearchRequest, success ? userId : default);
            return httpCreator.CreateResponse(propertySearchResponse);
        });

        group.MapGet("/details/{propertyId:int}", async (int propertyId,
            IPropertyService propertyService, HttpResponseCreator httpCreator) =>
        {
            var propertyDetailResponse = await propertyService.GetPropertyDetailsAsync(propertyId);
            return httpCreator.CreateResponse(propertyDetailResponse);
        });

        group.MapGet("/favorites", async (IPropertyService propertyService,
            HttpResponseCreator httpCreator, HttpContext context) =>
        {
            var userIdClaim = context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            var success = int.TryParse(userIdClaim?.Value, out int userId);
            if (!success)
            {
                return Results.Forbid();
            }
            var propertySearchResponse =
                await propertyService.GetFavoritePropertiesAsync(userId);
            return httpCreator.CreateResponse(propertySearchResponse);
        })
        .RequireAuthorization();
        
        return endpoints;
    }
}