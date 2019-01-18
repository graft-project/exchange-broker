using Graft.Infrastructure;
using System;
using System.ComponentModel.DataAnnotations;

namespace ExchangeBroker.Models.ExchangeViewModels
{
    public class ExchangeViewModel
    {
        [Display(Name = "Id")]
        public string ExchangeId { get; set; }

        [Display(Name = "Date")]
        public DateTime CreatedAt { get; set; }

        [Display(Name = "Status")]
        public PaymentStatus Status { get; set; }


        [Display(Name = "Sell Amount")]
        [DisplayFormat(DataFormatString = "{0:N9}", ApplyFormatInEditMode = true)]
        public decimal SellAmount { get; set; }

        [Display(Name = "Sell Currency")]
        public string SellCurrency { get; set; }


        [Display(Name = "Buy Amount")]
        [DisplayFormat(DataFormatString = "{0:N9}", ApplyFormatInEditMode = true)]
        public decimal BuyAmount { get; set; }

        [Display(Name = "Buy Currency")]
        public string BuyCurrency { get; set; }


        [Display(Name = "Sell To USD Rate")]
        [DisplayFormat(DataFormatString = "{0:N9}", ApplyFormatInEditMode = true)]
        public decimal SellToUsdRate { get; set; }

        [Display(Name = "Graft To USD Rate")]
        [DisplayFormat(DataFormatString = "{0:N9}", ApplyFormatInEditMode = true)]
        public decimal GraftToUsdRate { get; set; }


        [Display(Name = "Exchange Broker Fee")]
        [DisplayFormat(DataFormatString = "{0:N9}", ApplyFormatInEditMode = true)]
        public decimal ExchangeBrokerFee { get; set; }


        [Display(Name = "Buyer Wallet")]
        public string BuyerWallet { get; set; }


        [Display(Name = "Pay Wallet Address")]
        public string PayWalletAddress { get; set; }

        [Display(Name = "Pay Address Index")]
        public int PayAddressIndex { get; set; }


        [Display(Name = "Received Confirmations")]
        public int ReceivedConfirmations { get; set; }

        [Display(Name = "Received Amount")]
        [DisplayFormat(DataFormatString = "{0:N9}", ApplyFormatInEditMode = true)]
        public decimal ReceivedAmount { get; set; }


        [Display(Name = "Buyer Transaction Status")]
        public GraftTransactionStatus BuyerTransactionStatus { get; set; }
    }
}
