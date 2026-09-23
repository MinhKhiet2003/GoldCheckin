using Base.Common;
using Base.Models.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Base.Filters;

public class AuditResponseFilter(AuditContext audit) : IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.Result is ObjectResult result && (result.StatusCode ?? 200) is >= 200 and < 300)
        {
            // Danh sách cho phép: không ghi toàn bộ response hoặc token, không ghi lặp dữ liệu nhật ký.
            switch (result.Value)
            {
                case ApiResponse<UserResponse> response when response.Success:
                    audit.SetResponse(response.Data);
                    break;
                case ApiResponse<PagedResult<UserResponse>> response when response.Success:
                    audit.SetResponse(response.Data);
                    break;
                case ApiResponse<UploadFileResponse> response when response.Success:
                    audit.SetResponse(response.Data);
                    break;
                case ApiResponse<LoginResponse> response when response.Success && response.Data is not null:
                    audit.SetResponse(new
                    {
                        response.Data.Account,
                        response.Data.ExpiresAt
                    });
                    break;
            }
        }

        await next();
    }
}