using Graft.Infrastructure;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExchangeBroker.Models
{
    public class Payment
    {
        [Key]
        [Column(TypeName = "varchar(128)")]
        public string PaymentId { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        [Required]
        public PaymentStatus Status { get; set; }

        [Required]
        public decimal SaleAmount { get; set; }

        [Required]
        public string SaleCurrency { get; set; }

        [Required]
        public decimal PayToSaleRate { get; set; }

        [Required]
        public decimal GraftToSaleRate { get; set; }

        [Required]
        public string PayCurrency { get; set; }

        [Required]
        public decimal PayAmount { get; set; }

        [Required]
        public decimal ServiceProviderFee { get; set; }

        [Required]
        public decimal ExchangeBrokerFee { get; set; }

        [Required]
        public decimal MerchantAmount { get; set; }

        [Required]
        public string PayWalletAddress { get; set; }

        [Required]
        public int PayAddressIndex { get; set; }

        public int ReceivedConfirmations { get; set; }

        public decimal ReceivedAmount { get; set; }

        public string ServiceProviderWallet { get; set; }

        public string MerchantWallet { get; set; }

        public string MerchantTransactionId { get; set; }

        public GraftTransactionStatus MerchantTransactionStatus { get; set; }

        public string ProviderTransactionId { get; set; }

        public GraftTransactionStatus ProviderTransactionStatus { get; set; }
    }
}
