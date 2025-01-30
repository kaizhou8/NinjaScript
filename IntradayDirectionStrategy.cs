#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows.Media;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Indicators;

namespace NinjaTrader.NinjaScript.Strategies
{
    public class IntradayDirectionStrategy : Strategy
    {
        private DateTime sessionStart;
        private List<double> priceChanges;
        private double[] hourlyMomentum;
        private int[] hourlyCount;
        private bool isInTrade = false;
        private string currentDayDirection = "NEUTRAL";  // Current day's main direction
        private int consecutiveHigherHighs = 0;  // Count of consecutive higher highs
        private int consecutiveLowerLows = 0;    // Count of consecutive lower lows
        private List<double> recentHighs;        // Recent high prices
        private List<double> recentLows;         // Recent low prices
        private const int TREND_BARS = 3;        // Number of bars needed for trend determination
        
        private EMA ema5;   // Ultra-short term, 5 minutes
        private EMA ema13;  // Short term, 13 minutes
        private EMA ema30;  // Medium term, 30 minutes
        private RSI rsi;
        
        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"Intraday Trend Following Strategy";
                Name = "IntradayDirectionStrategy";
                MomentumPeriod = 20;
                RsiPeriod = 14;
                RsiSmooth = 3;
                MinStrength = 0.3;
                
                Calculate = Calculate.OnBarClose;
                EntriesPerDirection = 1;
                IsExitOnSessionCloseStrategy = true;
                
                // Initialize arrays and lists
                priceChanges = new List<double>();
                hourlyMomentum = new double[24];
                hourlyCount = new int[24];
                recentHighs = new List<double>();
                recentLows = new List<double>();
            }
            else if (State == State.Configure)
            {
                ema5 = EMA(Close, 5);    // 5-minute EMA
                ema13 = EMA(Close, 13);   // 13-minute EMA
                ema30 = EMA(Close, 30);   // 30-minute EMA
                rsi = RSI(Close, RsiPeriod, RsiSmooth);
                
                AddChartIndicator(ema5);
                AddChartIndicator(ema13);
                AddChartIndicator(ema30);
                AddChartIndicator(rsi);
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < 30) return;  // Wait for at least 30 bars

            // New trading day starts
            if (Bars.IsFirstBarOfSession)
            {
                sessionStart = Time[0];
                priceChanges.Clear();
                isInTrade = false;
                currentDayDirection = "NEUTRAL";
                consecutiveHigherHighs = 0;
                consecutiveLowerLows = 0;
                recentHighs.Clear();
                recentLows.Clear();
            }
            
            // Update recent highs and lows
            recentHighs.Add(High[0]);
            recentLows.Add(Low[0]);
            if (recentHighs.Count > TREND_BARS)
                recentHighs.RemoveAt(0);
            if (recentLows.Count > TREND_BARS)
                recentLows.RemoveAt(0);
            
            // Determine trend
            bool hasHigherHigh = false;
            bool hasLowerLow = false;
            
            if (recentHighs.Count > 1)
            {
                hasHigherHigh = High[0] > recentHighs.Take(recentHighs.Count - 1).Max();
                hasLowerLow = Low[0] < recentLows.Take(recentLows.Count - 1).Min();
            }
            
            if (hasHigherHigh)
            {
                consecutiveHigherHighs++;
                consecutiveLowerLows = 0;
            }
            else if (hasLowerLow)
            {
                consecutiveLowerLows++;
                consecutiveHigherHighs = 0;
            }
            
            // Dynamically update trend direction using EMA system
            bool isUpTrend = ema5[0] > ema13[0] && ema13[0] > ema30[0] && Close[0] > ema5[0];
            bool isDownTrend = ema5[0] < ema13[0] && ema13[0] < ema30[0] && Close[0] < ema5[0];
            
            if (isUpTrend && consecutiveHigherHighs >= 2)
                currentDayDirection = "UP";
            else if (isDownTrend && consecutiveLowerLows >= 2)
                currentDayDirection = "DOWN";
            
            // Calculate trend strength
            double priceChange = (Close[0] - Close[1]) / Close[1] * 100;
            priceChanges.Add(priceChange);
            if (priceChanges.Count > MomentumPeriod)
                priceChanges.RemoveAt(0);
            
            double momentum = priceChanges.Average();
            double volatility = CalculateVolatility(priceChanges);
            double trendStrength = momentum / volatility;
            
            // Update hourly momentum
            int hour = Time[0].Hour;
            hourlyMomentum[hour] = (hourlyMomentum[hour] * hourlyCount[hour] + priceChange) / (hourlyCount[hour] + 1);
            hourlyCount[hour]++;
            
            // Trading logic
            if (!isInTrade)
            {
                // Long entry conditions: Bullish EMA alignment + price breakout
                if (currentDayDirection == "UP" && 
                    isUpTrend &&
                    consecutiveHigherHighs >= 2 &&
                    rsi.Avg[0] > 45)  // Relaxed RSI requirement
                {
                    EnterLong("LONG");
                    Draw.ArrowUp(this, "Entry" + CurrentBar, false, 0, Low[0] - TickSize, Brushes.Green);
                    isInTrade = true;
                }
                // Short entry conditions: Bearish EMA alignment + price breakdown
                else if (currentDayDirection == "DOWN" && 
                         isDownTrend &&
                         consecutiveLowerLows >= 2 &&
                         rsi.Avg[0] < 55)  // Relaxed RSI requirement
                {
                    EnterShort("SHORT");
                    Draw.ArrowDown(this, "Entry" + CurrentBar, false, 0, High[0] + TickSize, Brushes.Red);
                    isInTrade = true;
                }
            }
            // Position management
            else
            {
                // Exit on reversal signals
                if (Position.MarketPosition == MarketPosition.Long && 
                    (Close[0] < ema5[0] || ema5[0] < ema13[0]))  // Use faster EMAs
                {
                    ExitLong("Reversal Exit");
                    isInTrade = false;
                }
                else if (Position.MarketPosition == MarketPosition.Short && 
                         (Close[0] > ema5[0] || ema5[0] > ema13[0]))  // Use faster EMAs
                {
                    ExitShort("Reversal Exit");
                    isInTrade = false;
                }
                
                // Force exit near session close
                TimeSpan timeToClose = Time[0].Date.AddHours(16) - Time[0];
                if (timeToClose.TotalMinutes <= 15)
                {
                    if (Position.MarketPosition == MarketPosition.Long)
                        ExitLong("Session Close");
                    else if (Position.MarketPosition == MarketPosition.Short)
                        ExitShort("Session Close");
                    isInTrade = false;
                }
            }
        }

        #region Properties
        [NinjaScriptProperty]
        [Range(10, 50)]
        [Display(Name = "Momentum Period", Description = "Period for calculating trend strength", Order = 1, GroupName = "Parameters")]
        public int MomentumPeriod { get; set; }

        [NinjaScriptProperty]
        [Range(2, 30)]
        [Display(Name = "RSI Period", Description = "RSI indicator period", Order = 2, GroupName = "Parameters")]
        public int RsiPeriod { get; set; }

        [NinjaScriptProperty]
        [Range(1, 10)]
        [Display(Name = "RSI Smooth", Description = "RSI smoothing period", Order = 3, GroupName = "Parameters")]
        public int RsiSmooth { get; set; }

        [NinjaScriptProperty]
        [Range(0.1, 1.0)]
        [Display(Name = "Min Strength", Description = "Minimum trend strength required for entry", Order = 4, GroupName = "Parameters")]
        public double MinStrength { get; set; }
        #endregion

        private double CalculateVolatility(List<double> changes)
        {
            double mean = changes.Average();
            double variance = changes.Sum(x => Math.Pow(x - mean, 2)) / changes.Count;
            return Math.Sqrt(variance);
        }
    }
}
