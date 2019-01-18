using Graft.Infrastructure;
using System;
using System.ComponentModel.DataAnnotations;

namespace ExchangeBroker.Models.PaymentViewModels
{
    public class PaymentViewModel
    {
        [Display(Name = "Id")]
        public string PaymentId { get; set; }

        [Display(Name = "Date")]
        public DateTime CreatedAt { get; set; }

        [Display(Name = "Status")]
        public PaymentStatus Status { get; set; }

        [Display(Name = "Sale Amount")]
        [DisplayFormat(DataFormatString = "{0:N9}", ApplyFormatInEditMode = true)]
        public decimal SaleAmount { get; set; }

        [Display(Name = "Sale Currency")]
        public string SaleCurrency { get; set; }

        [Display(Name = "Pay To Sale Rate")]
        [DisplayFormat(DataFormatString = "{0:N9}", ApplyFormatInEditMode = true)]
        public decimal PayToSaleRate { get; set; }

        [Display(Name = "Graft To Sale Rate")]
        [DisplayFormat(DataFormatString = "{0:N9}", ApplyFormatInEditMode = true)]
        public decimal GraftToSaleRate { get; set; }

        [Display(Name = "Pay Currency")]
        public string PayCurrency { get; set; }

        [Display(Name = "Pay Amount")]
        [DisplayFormat(DataFormatString = "{0:N9}", ApplyFormatInEditMode = true)]
        public decimal PayAmount { get; set; }

        [Display(Name = "Service Provider Fee")]
        [DisplayFormat(DataFormatString = "{0:N9}", ApplyFormatInEditMode = true)]
        public decimal ServiceProviderFee { get; set; }

        [Display(Name = "Exchange Broker Fee")]
        [DisplayFormat(DataFormatString = "{0:N9}", ApplyFormatInEditMode = true)]
        public decimal ExchangeBrokerFee { get; set; }

        [Display(Name = "Merchant Amount")]
        [DisplayFormat(DataFormatString = "{0:N9}", ApplyFormatInEditMode = true)]
        public decimal MerchantAmount { get; set; }

        [Display(Name = "Pay Wallet Address")]
        public string PayWalletAddress { get; set; }

        [Display(Name = "Pay Address Index")]
        public int PayAddressIndex { get; set; }

        [Display(Name = "Received Confirmations")]
        public int ReceivedConfirmations { get; set; }

        [Display(Name = "Received Amount")]
        [DisplayFormat(DataFormatString = "{0:N9}", ApplyFormatInEditMode = true)]
        public decimal ReceivedAmount { get; set; }

        [Display(Name = "Service Provider Wallet")]
        public string ServiceProviderWallet { get; set; }

        [Display(Name = "Merchant Wallet")]
        public string MerchantWallet { get; set; }

        [Display(Name = "Merchant Transaction Status")]
        public GraftTransactionStatus MerchantTransactionStatus { get; set; }

        [Display(Name = "Provider Transaction Status")]
        public GraftTransactionStatus ProviderTransactionStatus { get; set; }
    }
}
