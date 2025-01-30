# NinjaScript Custom Indicators

A collection of custom indicators for NinjaTrader 8.

## SmoothRenkoPlusIIBarType

An advanced Renko bar type implementation with smoothing capabilities for NinjaTrader 8.

### Example Chart
![SmoothRenkoPlusII Example](images/smooth_renko_example.png)
*Example of SmoothRenkoPlusII bar type showing smooth price movement and trend detection*

### Features
- Custom bar type based on Renko principles
- Smoothing algorithm for price movement
- Configurable parameters for bar size and calculation
- Real-time calculation support
- Compatible with NinjaTrader 8

### Installation
1. Open NinjaTrader 8
2. Tools -> Import -> NinjaScript Add-On
3. Select the downloaded file
4. Restart NinjaTrader 8

### Usage
1. Create a new chart
2. Select "SmoothRenkoPlusII" from the bar type dropdown
3. Configure parameters as needed:
   - Bar Size
   - Smoothing Factor
   - Other custom settings

### Requirements
- NinjaTrader 8
- .NET Framework 4.8 or higher

### Development
- Written in C# for NinjaTrader 8
- Uses NinjaTrader Core API
- Implements custom bar type calculations

## Contributing
Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change.

## License
[MIT](https://choosealicense.com/licenses/mit/)
