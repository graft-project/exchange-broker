using Graft.Infrastructure;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExchangeBroker.Models
{
    public class Exchange
    {
        [Key]
        [Column(TypeName = "varchar(128)")]
        public string ExchangeId { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        [Required]
        public PaymentStatus Status { get; set; }


        [Required]
        public decimal SellAmount { get; set; }

        [Required]
        public string SellCurrency { get; set; }


        [Required]
        public decimal BuyAmount { get; set; }

        [Required]
        public string BuyCurrency { get; set; }


        [Required]
        public decimal SellToUsdRate { get; set; }

        [Required]
        public decimal GraftToUsdRate { get; set; }


        [Required]
        public decimal ExchangeBrokerFee { get; set; }


        [Required]
        public string BuyerWallet { get; set; }


        [Required]
        public string PayWalletAddress { get; set; }

        [Required]
        public int PayAddressIndex { get; set; }


        public int ReceivedConfirmations { get; set; }

        public decimal ReceivedAmount { get; set; }


        public string InTxId { get; set; }

        public int InBlockNumber { get; internal set; }

        public PaymentStatus InTxStatus { get; set; }

        public string InTxStatusDescription { get; set; }


        public string OutTxId { get; set; }

        public int OutBlockNumber { get; internal set; }

        public PaymentStatus OutTxStatus { get; set; }

        public string OutTxStatusDescription { get; set; }


        [NotMapped]
        public List<EventItem> ProcessingEvents { get; set; }
    }
}
