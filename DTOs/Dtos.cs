namespace Store.DTOs;

public record ProductDto(
    int Id, string Name, decimal Price, string ImageUrl,
    int CategoryId, string CategoryName);

public record CartLineDto(
    int Id, int ProductId, string Name, decimal Price,
    int Quantity, decimal LineTotal);

public record CartDto(List<CartLineDto> Lines, decimal Total);