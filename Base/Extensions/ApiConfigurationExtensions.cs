using Base.Filters;
using Base.Common;
using Base.Models.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;

namespace Base.Extensions;

public static class ApiConfigurationExtensions
{
    public static IServiceCollection AddApiPresentation(this IServiceCollection services)
    {
        services.AddControllers(options =>
        {
            options.Filters.Add<AuditResponseFilter>();
            var messages = options.ModelBindingMessageProvider;
            messages.SetMissingBindRequiredValueAccessor(name => $"Vui lòng cung cấp trường {name}.");
            messages.SetMissingKeyOrValueAccessor(() => "Thiếu khóa hoặc giá trị bắt buộc.");
            messages.SetMissingRequestBodyRequiredValueAccessor(() => "Vui lòng gửi nội dung yêu cầu JSON.");
            messages.SetValueMustNotBeNullAccessor(value => "Trường này không được để trống.");
            messages.SetAttemptedValueIsInvalidAccessor((value, name) => $"Giá trị của trường {name} không hợp lệ.");
            messages.SetNonPropertyAttemptedValueIsInvalidAccessor(value => "Giá trị được cung cấp không hợp lệ.");
            messages.SetUnknownValueIsInvalidAccessor(name => $"Giá trị của trường {name} không hợp lệ.");
            messages.SetNonPropertyUnknownValueIsInvalidAccessor(() => "Giá trị được cung cấp không hợp lệ.");
            messages.SetValueIsInvalidAccessor(value => "Giá trị được cung cấp không hợp lệ.");
            messages.SetValueMustBeANumberAccessor(name => $"Trường {name} phải là số.");
            messages.SetNonPropertyValueMustBeANumberAccessor(() => "Giá trị được cung cấp phải là số.");
        })
        .ConfigureApiBehaviorOptions(options =>
        {
            options.SuppressMapClientErrors = true;
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = context.ModelState
                    .Where(entry => entry.Value?.Errors.Count > 0)
                    .ToDictionary(
                        entry => entry.Key,
                        entry => entry.Value!.Errors.Select(error =>
                        {
                            // Lỗi chuyển đổi JSON của framework có thể chứa tiếng Anh và chi tiết nội bộ.
                            if (error.Exception is not null
                                || string.IsNullOrWhiteSpace(error.ErrorMessage)
                                || error.ErrorMessage.All(character => character <= 127))
                            {
                                return "Dữ liệu không đúng định dạng hoặc không đúng kiểu dữ liệu yêu cầu.";
                            }

                            return error.ErrorMessage;
                        }).ToArray());

                return new BadRequestObjectResult(new ApiResponse<object>
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Success = false,
                    Message = "Dữ liệu đầu vào không hợp lệ.",
                    Errors = errors
                });
            };
        });
        services.Configure<JsonOptions>(options =>
        {
            options.AllowInputFormatterExceptionMessages = false;
        });
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, context, cancellationToken) =>
            {
                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
                document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "Dán accessToken nhận được từ API đăng nhập, không thêm tiền tố Bearer."
                };

                return Task.CompletedTask;
            });
            options.AddOperationTransformer((operation, context, cancellationToken) =>
            {
                var metadata = context.Description.ActionDescriptor.EndpointMetadata;
                var requiresAuthorization = metadata.OfType<IAuthorizeData>().Any()
                    && !metadata.OfType<IAllowAnonymous>().Any();

                if (requiresAuthorization)
                {
                    operation.Security =
                    [
                        new OpenApiSecurityRequirement
                        {
                            [new OpenApiSecuritySchemeReference("Bearer", context.Document)] = []
                        }
                    ];
                }

                return Task.CompletedTask;
            });
        });

        return services;
    }
}