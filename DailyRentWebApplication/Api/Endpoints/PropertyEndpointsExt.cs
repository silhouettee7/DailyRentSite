using System.Security.Claims;
using Api.Models;
using Api.Services;
using Api.Utils;
using AutoMapper;
using Domain.Abstractions.Services;
using Domain.Models.Dtos;
using Domain.Models.Dtos.Amenity;
using Domain.Models.Dtos.Image;
using Domain.Models.Dtos.Property;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Api.Endpoints;

public static class PropertyEndpointsExt
{
    public static IEndpointRouteBuilder MapPropertiesEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/properties")
            .WithTags("Properties");

        group.MapPost("/create", async (IPropertyService propertyService, HttpResponseCreator httpCreator, HttpContext context, IMapper mapper) =>
        {
            var userIdClaim = context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            var success = int.TryParse(userIdClaim?.Value, out int userId);
            if (!success)
            {
                return Results.Forbid();
            }
            var form = await context.Request.ReadFormAsync();

            var title = form["Title"];
            var description = form["Description"];
            var pricePerDay = decimal.Parse(form["PricePerDay"]);
            var maxGuests = int.Parse(form["MaxGuests"]);
            var bedrooms = int.Parse(form["Bedrooms"]);
            var petsAllowed = bool.Parse(form["PetsAllowed"]);

            var amenities = new List<AmenityCreateRequest>();
            int i = 0;
            while (form.ContainsKey($"Amenities[{i}].Name"))
            {
                amenities.Add(new AmenityCreateRequest
                {
                    Name = form[$"Amenities[{i}].Name"]
                });
                i++;
            }

            var location = new LocationCreateRequest
            {
                Country = form["Location.Country"],
                City = form["Location.City"],
                Address = form["Location.Address"],
                Street = form["Location.Street"],
                House = form["Location.House"],
                District = form["Location.District"],
                Latitude = double.Parse(form["Location.Latitude"]),
                Longitude = double.Parse(form["Location.Longitude"]),
            };

            var property = new PropertyCreate
            {
                Title = title!,
                Description = description!,
                PricePerDay = pricePerDay,
                MaxGuests = maxGuests,
                Bedrooms = bedrooms,
                PetsAllowed = petsAllowed,
                Amenities = amenities!,
                Location = location!
            };
            var propertyCreateRequest = mapper.Map<PropertyCreateRequest>(property);
            var mainFile = form.Files["mainFile"];
            propertyCreateRequest.MainImage = new ImageFileRequest
            {
                FileName = mainFile?.FileName,
                Stream = mainFile?.OpenReadStream(),
                ContentType = mainFile.ContentType,
            };
            propertyCreateRequest.PropertyImages = form.Files.GetFiles("files")
                .Select(f => new ImageFileRequest 
                { 
                    FileName = f?.FileName,
                    Stream = f?.OpenReadStream(),
                    ContentType = f.ContentType, 
                })
                .ToList();
            var propertyIdResult = await propertyService.CreatePropertyAsync(propertyCreateRequest, userId);
            return httpCreator.CreateResponse(propertyIdResult);
        })
        .DisableAntiforgery()
        .RequireAuthorization();

        group.MapPost("/search", async ([FromBody]PropertySearchRequest propertySearchRequest, IPropertyService propertyService,
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

        group.MapPatch("/like/{propertyId:int}", async (int propertyId, IPropertyService propertyService,
            HttpResponseCreator creator, HttpContext context) =>
        {
            var userIdClaim = context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            var success = int.TryParse(userIdClaim?.Value, out int userId);
            if (!success)
            {
                return Results.Forbid();
            }

            var result = await propertyService.LikePropertyAsync(propertyId, userId);
            return creator.CreateResponse(result);
        });
        
        group.MapPatch("/dislike/{propertyId:int}", async (int propertyId, IPropertyService propertyService,
            HttpResponseCreator creator, HttpContext context) =>
        {
            var userIdClaim = context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            var success = int.TryParse(userIdClaim?.Value, out int userId);
            if (!success)
            {
                return Results.Forbid();
            }

            var result = await propertyService.DislikePropertyAsync(propertyId, userId);
            return creator.CreateResponse(result);
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

        group.MapGet("/own", async (IPropertyService propertyService,
            HttpResponseCreator httpCreator, HttpContext context) =>
        {
            var userIdClaim = context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            var success = int.TryParse(userIdClaim?.Value, out int userId);
            if (!success)
            {
                return Results.Forbid();
            }
            var propertySearchResponse =
                await propertyService.GetOwnerPropertyAsync(userId);
            
            return httpCreator.CreateResponse(propertySearchResponse);
        })
        .RequireAuthorization();
        
        return endpoints;
    }
}