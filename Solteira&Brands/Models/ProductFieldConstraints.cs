namespace Solteira_Brands.Models
{
    /// <summary>Shared limits for <c>tbl_products</c> and admin save payloads.</summary>
    public static class ProductFieldConstraints
    {
        public const int MaxNameLength = 30;

        /// <summary>Stones, material, weight, length on admin inventory forms.</summary>
        public const int MaxSpecFieldLength = 50;
    }
}
