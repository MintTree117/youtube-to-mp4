using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace YouToMp4Avalonia.Services;

public sealed class FFmpegChecker : BaseService
{
    bool? _isFFmpegInstalled;

    public async Task<bool> CheckFFmpegInstallationAsync( string ffmpegFilepath )
    {
        _isFFmpegInstalled ??= await IsFFmpegInstalledAsync( ffmpegFilepath );
        return _isFFmpegInstalled.Value;
    }
    async Task<bool> IsFFmpegInstalledAsync( string ffmpegFilepath )
    {
        // FFmpeg process
        bool processStarted = false;
        using Process process = new();
        process.StartInfo.FileName = ffmpegFilepath;
        process.StartInfo.Arguments = "-version";
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.UseShellExecute = false; // TODO: Test if need true
        process.StartInfo.CreateNoWindow = true;
        
        try
        {
            processStarted = process.Start();

            if ( !processStarted )
            {
                Logger.LogWithConsole( $"FFmpeg initialization failed to start process!" );
                return false;
            }

            // Read the output to ensure the command was executed
            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();
            
            await process.WaitForExitAsync();

            bool success = process.ExitCode == 0 && !string.IsNullOrWhiteSpace( output );

            if ( !success )
                Logger.LogWithConsole( $"FFmpeg initialization fail: {error}" );

            return success;
        }
        catch ( Exception e )
        {
            Logger.LogWithConsole( ExString( e ) );
            return false;
        }
        finally
        {
            if ( processStarted && !process.HasExited )
                process.Kill();
        }
    }
}