using System.Security.Claims;
using Api.Models;
using Api.Utils;
using AutoMapper;
using Domain.Abstractions.Services;
using Domain.Models.Dtos.CompensationRequest;

namespace Api.Endpoints;


public static class CompensationRequestsEndpoints
{
    public static IEndpointRouteBuilder MapCompensationRequestsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/compensation-requests")
            .WithTags("Compensation Requests")
            .RequireAuthorization();
        
        group.MapPost("/", async (
            CompensationRequestCreate request,
            ICompensationRequestService service,
            HttpResponseCreator responseCreator,
            IMapper mapper,
            HttpContext context) =>
        {
            var userIdClaim = context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            var success = int.TryParse(userIdClaim?.Value, out int userId);
            if (!success)
            {
                return Results.Forbid();
            }
            var result = await service.CreateCompensationRequest(mapper.Map<CompensationRequestDto>(request), userId);
            return responseCreator.CreateResponse(result);
        });
        
        group.MapDelete("/{id}", async (
            int id,
            ICompensationRequestService service,
            HttpResponseCreator responseCreator) =>
        {
            var result = await service.DeleteCompensationRequest(id);
            return responseCreator.CreateResponse(result);
        });
        
        group.MapGet("/booking/{bookingId}", async (
            int bookingId,
            ICompensationRequestService service,
            HttpResponseCreator responseCreator) =>
        {
            var result = await service.GetCompensationRequestByBookingId(bookingId);
            return responseCreator.CreateResponse(result);
        });
        
        group.MapPatch("/{id}/reject", async (
            int id,
            ICompensationRequestService service,
            HttpResponseCreator responseCreator,
            HttpContext context) =>
        {
            var userIdClaim = context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            var success = int.TryParse(userIdClaim?.Value, out int userId);
            if (!success)
            {
                return Results.Forbid();
            }
            var result = await service.RejectCompensationRequest(id, userId);
            return responseCreator.CreateResponse(result);
        });

        group.MapPatch("/{id}/approve", async (
            int id,
            decimal amount,
            ICompensationRequestService service,
            HttpResponseCreator responseCreator,
            HttpContext context) =>
        {
            var userIdClaim = context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            var success = int.TryParse(userIdClaim?.Value, out int userId);
            if (!success)
            {
                return Results.Forbid();
            }
            var result = await service.ApproveCompensationRequest(userId, id, amount);
            return responseCreator.CreateResponse(result);
        });

        return endpoints;
    }
}