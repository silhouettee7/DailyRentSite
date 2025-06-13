using System.Security.Claims;
using Api.Models;
using Api.Utils;
using AutoMapper;
using Domain.Abstractions.Services;
using Domain.Models.Dtos.CompensationRequest;
using Domain.Models.Dtos.Image;

namespace Api.Endpoints;


public static class CompensationRequestsEndpoints
{
    public static IEndpointRouteBuilder MapCompensationRequestsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/compensation-requests")
            .WithTags("Compensation Requests")
            .RequireAuthorization();
        
        group.MapPost("/", async (
            HttpContext context,
            ICompensationRequestService service,
            HttpResponseCreator responseCreator,
            IMapper mapper) =>
        {
            // Проверка аутентификации пользователя
            var userIdClaim = context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim?.Value, out int userId))
            {
                return Results.Forbid();
            }

            // Получаем форму из запроса
            var form = await context.Request.ReadFormAsync();
    
            // Парсим данные из формы
            if (!decimal.TryParse(form["RequestedAmount"], out decimal requestedAmount) ||
                !int.TryParse(form["BookingId"], out int bookingId))
            {
                return Results.BadRequest("Invalid request data");
            }

            var description = form["Description"];
    
            // Получаем файлы
            var proofPhotos = form.Files.GetFiles("ProofPhotos");

            // Создаем DTO
            var requestDto = new CompensationRequestDto
            {
                Description = description,
                RequestedAmount = requestedAmount,
                BookingId = bookingId,
                ProofPhotos = proofPhotos.Select(f => new ImageFileRequest
                {
                    FileName = f.FileName,
                    ContentType = f.ContentType, 
                    Stream= f.OpenReadStream()
                }).ToList()
            };

            // Вызываем сервис
            var result = await service.CreateCompensationRequest(requestDto, userId);
            return responseCreator.CreateResponse(result);
        });
        group.MapGet("/{requestId}",
            async (int requestId, ICompensationRequestService service, HttpResponseCreator creator) =>
            {
                var request = await service.GetCompensationRequestById(requestId);
                return creator.CreateResponse(request);
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

        group.MapPatch("/{id}/approve/{amount}", async (
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