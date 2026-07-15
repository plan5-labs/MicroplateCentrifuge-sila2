using AxCentrifugeLoaderLib;
using Grpc.Core;
using MicroplateCentrifugeCommandExecutor.Models;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using static MicroplateCentrifugeCommandExecutor.MicroplateCentrifugeCommandExecutorService;

namespace MicroplateCentrifugeCommandExecutor.Services
{
    public class MicroplateCentrifugeCommandExecutorServiceImpl : MicroplateCentrifugeCommandExecutorServiceBase
    {
        private static readonly MicroplateCentrifugeCommandExecutorServiceImpl _instance = new MicroplateCentrifugeCommandExecutorServiceImpl();

        // For safety reasons, this software enforces mutual exclusion to prevent concurrent commands.
        private static readonly string s_centrifugeMutexName = "Global\\MicroplateCentrifugeService";
        private static readonly Mutex s_centrifugeMutex = new Mutex(false, s_centrifugeMutexName);

        private static AxCentrifugeLoader s_centrifugeLoader = null;
        private static string s_loaderProfileName;
        private static bool s_isInitialized = false;

        /// <summary>
        /// The command name running
        /// </summary>
        private static string s_runningCommandName = string.Empty;

        // For subscribe status
        private static MicroplateCentrifugeStatus s_status = MicroplateCentrifugeStatus.GetInstance();
        private readonly Channel<SubscribeStatusResponse> _queue = System.Threading.Channels.Channel.CreateUnbounded<SubscribeStatusResponse>(
            new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

        private const string NO_TASK_TO_ABORT = "No task to abort!";

        private MicroplateCentrifugeCommandExecutorServiceImpl() { }

        ~MicroplateCentrifugeCommandExecutorServiceImpl()
        {
            s_centrifugeMutex.Close();
        }

        #region gRPC Request
        public override async Task<OpenDoorResponse> OpenDoor(OpenDoorRequest request, ServerCallContext context)
        {
            Log.Information("OpenDoor start.");
            Log.Information($"BucketNumber: {request.BucketNumber}");

            await ExecuteWithMutexAsync(OpenDoorAction, request, "OpenDoor", MicroplateCentrifugeStatus.StatusCode.Idle);

            Log.Information("OpenDoor complete.");
            return new OpenDoorResponse();
        }

        public override async Task<CloseDoorResponse> CloseDoor(CloseDoorRequest request, ServerCallContext context)
        {
            Log.Information("CloseDoor start.");
            await ExecuteWithMutexAsync(CloseDoorAction, request, "CloseDoor", MicroplateCentrifugeStatus.StatusCode.Idle);
            Log.Information("CloseDoor complete.");
            return new CloseDoorResponse();
        }

        public override async Task<SpinCycleResponse> SpinCycle(SpinCycleRequest request, ServerCallContext context)
        {
            Log.Information("SpinCycle start.");
            Log.Information($"VelocityPercent: {request.VelocityPercent} " +
                            $"AccelerationPercent: {request.AccelerationPercent} " +
                            $"DecelerationPercent: {request.DecelerationPercent} " +
                            $"TimerMode: {request.TimerMode} " +
                            $"Time: {request.Time} " +
                            $"BucketNumberToLoad: {request.BucketNumberToLoad} " +
                            $"BucketNumberToUnload: {request.BucketNumberToUnload} " +
                            $"GripperOffsetToLoad: {request.GripperOffsetToLoad} " +
                            $"GripperOffsetToUnload: {request.GripperOffsetToUnload} " +
                            $"PlateHeightToLoad: {request.PlateHeightToLoad} " +
                            $"PlateHeightToUnload: {request.PlateHeightToUnload} " +
                            $"SpeedToLoad: {request.SpeedToLoad} " +
                            $"SpeedToUnload: {request.SpeedToUnload} " +
                            $"OptionsToLoad: {request.OptionsToLoad} " +
                            $"OptionsToUnload: {request.OptionsToUnload}");

            await ExecuteWithMutexAsync(SpinCycleAction, request, "SpinCycle", MicroplateCentrifugeStatus.StatusCode.Idle);

            Log.Information("SpinCycle complete.");
            return new SpinCycleResponse();
        }

        public override Task<StopSpinCycleResponse> StopSpinCycle(StopSpinCycleRequest request, ServerCallContext context)
        {
            Log.Information("StopSpinCycle start.");
            Log.Information($"BucketNumber: {request.BucketNumber}");

            CallStopSpinCycle((short)request.BucketNumber);

            Log.Information("StopSpinCycle complete.");
            return Task.FromResult(new StopSpinCycleResponse());
        }

        public override async Task<ResetResponse> Reset(ResetRequest request, ServerCallContext context)
        {
            Log.Information("Reset start.");

            var expectedStatus = new List<MicroplateCentrifugeStatus.StatusCode> { MicroplateCentrifugeStatus.StatusCode.Error, MicroplateCentrifugeStatus.StatusCode.Idle };
            await ExecuteWithMutexAsync(ResetAction, request, "Reset", expectedStatus);

            Log.Information("Reset complete.");
            return new ResetResponse();
        }

        public override async Task<HomeResponse> Home(HomeRequest request, ServerCallContext context)
        {
            Log.Information("Home start.");
            await ExecuteWithMutexAsync(HomeAction, request, "Home", MicroplateCentrifugeStatus.StatusCode.Idle);
            Log.Information("Home complete.");
            return new HomeResponse();
        }

        public override async Task<ParkResponse> Park(ParkRequest request, ServerCallContext context)
        {
            Log.Information("Park start.");
            await ExecuteWithMutexAsync(ParkAction, request, "Park", MicroplateCentrifugeStatus.StatusCode.Idle);
            Log.Information("Park complete.");
            return new ParkResponse();
        }

        public override async Task<LoadPlateResponse> LoadPlate(LoadPlateRequest request, ServerCallContext context)
        {
            Log.Information("LoadPlate start.");
            Log.Information($"BucketNumber: {request.BucketNumber} " +
                            $"GripperOffset: {request.GripperOffset} " +
                            $"PlateHeight: {request.PlateHeight} " +
                            $"Speed: {request.Speed} " +
                            $"Options: {request.Options}");

            await ExecuteWithMutexAsync(LoadPlateAction, request, "LoadPlate", MicroplateCentrifugeStatus.StatusCode.Idle);

            Log.Information("LoadPlate complete.");
            return new LoadPlateResponse();
        }

        public override async Task<UnloadPlateResponse> UnloadPlate(UnloadPlateRequest request, ServerCallContext context)
        {
            Log.Information("UnloadPlate start.");
            Log.Information($"BucketNumber: {request.BucketNumber} " +
                            $"GripperOffset: {request.GripperOffset} " +
                            $"PlateHeight: {request.PlateHeight} " +
                            $"Speed: {request.Speed} " +
                            $"Options: {request.Options}");

            await ExecuteWithMutexAsync(UnloadPlateAction, request, "UnloadPlate", MicroplateCentrifugeStatus.StatusCode.Idle);

            Log.Information("UnloadPlate complete.");
            return new UnloadPlateResponse();
        }

        public override Task<EnumerateProfilesResponse> EnumerateProfiles(EnumerateProfilesRequest request, ServerCallContext context)
        {
            Log.Information("EnumerateProfiles start.");

            CheckInitialized();
            var expectedStatuses = new List<MicroplateCentrifugeStatus.StatusCode> { MicroplateCentrifugeStatus.StatusCode.Idle, MicroplateCentrifugeStatus.StatusCode.Running };
            CheckCurrentStatusValid(expectedStatuses);

            var profiles = s_centrifugeLoader.EnumerateProfiles() as string[];
            var response = new EnumerateProfilesResponse();
            response.ProfileNames.AddRange(profiles);

            Log.Information("EnumerateProfiles complete.");
            Log.Information($"ProfileNames: {string.Join(", ", profiles)}");
            return Task.FromResult(response);
        }

        public override Task<GetActiveXVersionResponse> GetActiveXVersion(GetActiveXVersionRequest request, ServerCallContext context)
        {
            Log.Information("GetActiveXVersion start.");

            CheckInitialized();
            var expectedStatuses = new List<MicroplateCentrifugeStatus.StatusCode> { MicroplateCentrifugeStatus.StatusCode.Idle, MicroplateCentrifugeStatus.StatusCode.Running };
            CheckCurrentStatusValid(expectedStatuses);

            var activeXVersion = s_centrifugeLoader.GetActiveXVersion();
            Log.Information("GetActiveXVersion complete.");
            Log.Information($"ActiveXVersion: {activeXVersion}");
            return Task.FromResult(new GetActiveXVersionResponse { ActiveXVersion = activeXVersion });
        }

        public override Task<GetCentrifugeActiveXVersionResponse> GetCentrifugeActiveXVersion(GetCentrifugeActiveXVersionRequest request, ServerCallContext context)
        {
            Log.Information("GetCentrifugeActiveXVersion start.");

            CheckInitialized();
            var expectedStatuses = new List<MicroplateCentrifugeStatus.StatusCode> { MicroplateCentrifugeStatus.StatusCode.Idle, MicroplateCentrifugeStatus.StatusCode.Running };
            CheckCurrentStatusValid(expectedStatuses);

            var centrifugeActiveXVersion = s_centrifugeLoader.GetCentrifugeActiveXVersion();
            Log.Information("GetCentrifugeActiveXVersion complete.");
            Log.Information($"CentrifugeActiveXVersion: {centrifugeActiveXVersion}");
            return Task.FromResult(new GetCentrifugeActiveXVersionResponse { CentrifugeActiveXVersion = centrifugeActiveXVersion });
        }

        public override Task<GetCentrifugeHardwareVersionResponse> GetCentrifugeHardwareVersion(GetCentrifugeHardwareVersionRequest request, ServerCallContext context)
        {
            Log.Information("GetCentrifugeHardwareVersion start.");

            CheckInitialized();
            var expectedStatuses = new List<MicroplateCentrifugeStatus.StatusCode> { MicroplateCentrifugeStatus.StatusCode.Idle, MicroplateCentrifugeStatus.StatusCode.Running };
            CheckCurrentStatusValid(expectedStatuses);

            var centrifugeHardwareVersion = s_centrifugeLoader.GetCentrifugeHardwareVersion();
            Log.Information("GetCentrifugeHardwareVersion complete.");
            Log.Information($"CentrifugeHardwareVersion: {centrifugeHardwareVersion}");
            return Task.FromResult(new GetCentrifugeHardwareVersionResponse { CentrifugeHardwareVersion = centrifugeHardwareVersion });
        }

        public override Task<GetFirmwareVersionResponse> GetFirmwareVersion(GetFirmwareVersionRequest request, ServerCallContext context)
        {
            Log.Information("GetFirmwareVersion start.");

            CheckInitialized();
            var expectedStatuses = new List<MicroplateCentrifugeStatus.StatusCode> { MicroplateCentrifugeStatus.StatusCode.Idle, MicroplateCentrifugeStatus.StatusCode.Running };
            CheckCurrentStatusValid(expectedStatuses);

            var firmwareVersion = s_centrifugeLoader.GetFirmwareVersion();
            Log.Information("GetFirmwareVersion complete.");
            Log.Information($"FirmwareVersion: {firmwareVersion}");
            return Task.FromResult(new GetFirmwareVersionResponse { FirmwareVersion = firmwareVersion });
        }

        public override Task<GetHardwareVersionResponse> GetHardwareVersion(GetHardwareVersionRequest request, ServerCallContext context)
        {
            Log.Information("GetHardwareVersion start.");

            CheckInitialized();
            var expectedStatuses = new List<MicroplateCentrifugeStatus.StatusCode> { MicroplateCentrifugeStatus.StatusCode.Idle, MicroplateCentrifugeStatus.StatusCode.Running };
            CheckCurrentStatusValid(expectedStatuses);

            var hardwareVersion = s_centrifugeLoader.GetHardwareVersion();
            Log.Information("GetHardwareVersion complete.");
            Log.Information($"HardwareVersion: {hardwareVersion}");
            return Task.FromResult(new GetHardwareVersionResponse { HardwareVersion = hardwareVersion });
        }

        public override async Task SubscribeStatus(SubscribeStatusRequest request, IServerStreamWriter<SubscribeStatusResponse> responseStream, ServerCallContext context)
        {
            void handler(object s, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == nameof(MicroplateCentrifugeStatus.Status) && s is MicroplateCentrifugeStatus)
                {
                    _queue.Writer.TryWrite(new SubscribeStatusResponse { Status = (int)(s as MicroplateCentrifugeStatus).Status });
                }
            }

            Log.Information($"Start to subscribe Status. Current Status: {s_status.Status}");

            _queue.Writer.TryWrite(new SubscribeStatusResponse { Status = (int)s_status.Status });
            s_status.PropertyChanged += handler;
            try
            {
                while (!context.CancellationToken.IsCancellationRequested)
                {
                    if (await _queue.Reader.WaitToReadAsync(context.CancellationToken) && _queue.Reader.TryRead(out var update))
                    {
                        await responseStream.WriteAsync(update);
                    }
                }
            }
            finally
            {
                s_status.PropertyChanged -= handler;
                Log.Information("Stop subscribing Status.");
            }
        }
        #endregion

        #region Service Basics

        #region Initialize
        /// <summary>
        /// Get the profile name from config file and try to connect the Microplate Centrifuge.
        /// </summary>
        /// <param name="form"></param>
        /// <param name="appConfiguration"></param>
        public void Initialize(MicroplateCentrifugeForm form, IConfigurationSection appConfiguration)
        {
            s_centrifugeLoader = form.GetLoader();

            s_loaderProfileName = appConfiguration["loaderProfileName"];
            Log.Information($"loaderProfileName read: {s_loaderProfileName}");

            Connect();
            var status = s_isInitialized ? MicroplateCentrifugeStatus.StatusCode.Idle : MicroplateCentrifugeStatus.StatusCode.NotConnected;
            s_status.UpdateStatus(status);
        }

        internal void Connect()
        {
            int loaderResult = s_centrifugeLoader.Initialize(s_loaderProfileName);

            if (loaderResult == 0)
            {
                s_isInitialized = true;
                Log.Information($"Device initialized.");
            }
            else
            {
                Log.Debug($"Initialize failed. Return : loader = {loaderResult}");
                s_isInitialized = false;
                Log.Warning($"Failed to loader initialize: {s_centrifugeLoader.GetLastError()}");
            }
        }
        #endregion

        internal void Close()
        {
            Log.Information("Closing device.");
            int loaderResult = s_centrifugeLoader.Close();

            if (loaderResult != 0)
            {
                s_status.UpdateStatus(MicroplateCentrifugeStatus.StatusCode.Error);
                Log.Debug($"Loader Close failed with return {loaderResult}");
                Log.Warning($"Failed to close connection: {s_centrifugeLoader.GetLastError()}");
            }
            else
            {
                s_status.UpdateStatus(MicroplateCentrifugeStatus.StatusCode.NotConnected);
                Log.Information("Closed device successfully.");
            }
        }

        public static MicroplateCentrifugeCommandExecutorServiceImpl GetService()
        {
            return _instance;
        }
        #endregion

        #region Common methods
        private void HandleCentrifugeError(int result)
        {
            s_status.UpdateStatus(MicroplateCentrifugeStatus.StatusCode.Error);

            string lastErrorMessage = s_centrifugeLoader.GetLastError();
            var errorMessage = $"Error has occured. ErrorCode:{result} ErrorMessage:{lastErrorMessage}";
            Log.Error(errorMessage);
            throw new RpcException(new Status(StatusCode.Internal, errorMessage));
        }

        /// <summary>
        /// Perform initialization and status checks, ensure mutual exclusion, and call the actual processing only if everything completes successfully.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="concreteAction"></param>
        /// <param name="param"></param>
        /// <param name="commandName"></param>
        /// <param name="expectedStatus"></param>
        private async Task ExecuteWithMutexAsync<T>(Action<T> concreteAction, T param, string commandName, MicroplateCentrifugeStatus.StatusCode expectedStatus)
        {
            await ExecuteWithMutexAsync(concreteAction, param, commandName, new List<MicroplateCentrifugeStatus.StatusCode> { expectedStatus });
        }

        /// <summary>
        /// Perform initialization and status checks, ensure mutual exclusion, and call the actual processing only if everything completes successfully.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="concreteAction"></param>
        /// <param name="param"></param>
        /// <param name="commandName"></param>
        /// <param name="expectedStatuses"></param>
        private async Task ExecuteWithMutexAsync<T>(Action<T> concreteAction, T param, string commandName, List<MicroplateCentrifugeStatus.StatusCode> expectedStatuses)
        {
            CheckInitialized();
            CheckCurrentStatusValid(expectedStatuses);

            await Task.Run(() =>
            {
                bool isMutexGained = s_centrifugeMutex.WaitOne(0);
                if (!isMutexGained)
                {
                    var errorMessage = $"{s_runningCommandName} is running.";
                    Log.Error(errorMessage);
                    throw new RpcException(new Status(StatusCode.Unavailable, errorMessage));
                }
                s_runningCommandName = commandName;

                try
                {
                    concreteAction(param);
                }
                finally
                {
                    s_centrifugeMutex.ReleaseMutex();
                    s_runningCommandName = string.Empty;
                }
            });
        }

        private void CheckInitialized()
        {
            if (!s_isInitialized)
            {
                Log.Information("Retrying to initialize device.");
                Connect();
                if (!s_isInitialized)
                {
                    var errorMessage = "MicroplateCentrifuge is not initialized.";
                    Log.Error(errorMessage);
                    throw new RpcException(new Status(StatusCode.FailedPrecondition, errorMessage));
                }
            }
        }

        private void CheckCurrentStatusValid(MicroplateCentrifugeStatus.StatusCode validStatus)
        {
            CheckCurrentStatusValid(new List<MicroplateCentrifugeStatus.StatusCode> { validStatus });
        }

        private void CheckCurrentStatusValid(List<MicroplateCentrifugeStatus.StatusCode> validStatuses)
        {
            var validStatusesStr = string.Join(", ", validStatuses.Select(s => Enum.GetName(typeof(MicroplateCentrifugeStatus.StatusCode), s)));
            Log.Debug($"Check current status is valid or not. Expected: {validStatusesStr}");

            if (!s_status.IsStatusValid(validStatuses))
            {
                var errorMessage = $"InvalidStatus. Expected: {validStatusesStr} / Actual: {Enum.GetName(typeof(MicroplateCentrifugeStatus.StatusCode), s_status.Status)}";
                Log.Error(errorMessage);
                throw new RpcException(new Status(StatusCode.FailedPrecondition, errorMessage));
            }
        }
        #endregion

        #region OpenDoor
        private void OpenDoorAction(OpenDoorRequest request)
        {
            Log.Debug("OpenDoorAction start.");
            CallOpenDoor((short)request.BucketNumber);
            Log.Debug("OpenDoorAction complete.");
        }

        internal void CallOpenDoor(short bucketNumber)
        {
            Log.Debug("CallOpenDoor start.");
            s_status.UpdateStatus(MicroplateCentrifugeStatus.StatusCode.Running);

            int result = s_centrifugeLoader.OpenDoor(bucketNumber);
            if (result != 0)
            {
                Log.Debug($"OpenDoor failed with return {result}");
                HandleCentrifugeError(result);
            }

            s_status.UpdateStatus(MicroplateCentrifugeStatus.StatusCode.Idle);
            Log.Debug("CallOpenDoor complete.");

        }
        #endregion

        #region CloseDoor
        private void CloseDoorAction(CloseDoorRequest request)
        {
            Log.Debug("CloseDoorAction start.");
            CallCloseDoor();
            Log.Debug("CloseDoorAction complete.");
        }

        internal void CallCloseDoor()
        {
            Log.Debug("CallCloseDoor start.");
            s_status.UpdateStatus(MicroplateCentrifugeStatus.StatusCode.Running);

            int result = s_centrifugeLoader.CloseDoor();
            if (result != 0)
            {
                Log.Debug($"CloseDoor failed with return {result}");
                HandleCentrifugeError(result);
            }

            s_status.UpdateStatus(MicroplateCentrifugeStatus.StatusCode.Idle);
            Log.Debug("CallCloseDoor complete.");
        }
        #endregion

        #region SpinCycle
        private void SpinCycleAction(SpinCycleRequest request)
        {
            Log.Debug("SpinCycleAction start.");
            CallSpinCycle(request);
            Log.Debug("SpinCycleAction complete.");
        }

        internal void CallSpinCycle(SpinCycleRequest request)
        {
            Log.Debug("CallSpinCycle start.");
            s_status.UpdateStatus(MicroplateCentrifugeStatus.StatusCode.Running);

            int result = s_centrifugeLoader.SpinCycle(
                request.VelocityPercent,
                request.AccelerationPercent,
                request.DecelerationPercent,
                request.TimerMode,
                request.Time,
                request.BucketNumberToLoad,
                request.BucketNumberToUnload,
                request.GripperOffsetToLoad,
                request.GripperOffsetToUnload,
                request.PlateHeightToLoad,
                request.PlateHeightToUnload,
                request.SpeedToLoad,
                request.SpeedToUnload,
                request.OptionsToLoad,
                request.OptionsToUnload
                );
            if (result != 0)
            {
                Log.Debug($"SpinCycle failed with return {result}");
                HandleCentrifugeError(result);
            }

            s_status.UpdateStatus(MicroplateCentrifugeStatus.StatusCode.Idle);
            Log.Debug("CallSpinCycle complete.");
        }
        #endregion

        #region StopSpinCycle
        internal void CallStopSpinCycle(short bucketNumber)
        {
            Log.Debug("CallStopSpinCycle start.");

            CheckInitialized();
            CheckCurrentStatusValid(MicroplateCentrifugeStatus.StatusCode.Running);

            s_status.UpdateStatus(MicroplateCentrifugeStatus.StatusCode.Running);

            int result = s_centrifugeLoader.StopSpinCycle(bucketNumber);
            if (result != 0)
            {
                s_status.UpdateStatus(MicroplateCentrifugeStatus.StatusCode.Error);
                Log.Debug($"StopSpinCycle failed with return {result}");
                string lastErrorMessage = s_centrifugeLoader.GetLastError();
                var errorMessage = $"StopSpinCycle failed because of the following error: {lastErrorMessage}";
                Log.Error(errorMessage);
                throw new RpcException(new Status(StatusCode.Internal, errorMessage));
            }
            else
            {
                s_status.UpdateStatus(MicroplateCentrifugeStatus.StatusCode.Idle);
                Log.Information("The running cycle has been stopped.");
            }

            Log.Debug("CallStopSpinCycle complete.");
        }
        #endregion

        #region Reset
        private void ResetAction(ResetRequest request)
        {
            Log.Debug("ResetAction start.");
            CallAbort();
            Log.Debug("ResetAction complete.");
        }

        internal void CallAbort()
        {
            Log.Debug("CallAbort start.");

            int result = s_centrifugeLoader.Abort();
            if (result != 0)
            {
                var abortErrorMessage = s_centrifugeLoader.GetLastError();
                if (abortErrorMessage == NO_TASK_TO_ABORT)
                {
                    // If no errors can be resolved by Abort(), "No task to abort!" will be returned.
                    Log.Debug("No task to abort.");
                }
                else
                {
                    s_status.UpdateStatus(MicroplateCentrifugeStatus.StatusCode.Error);

                    Log.Debug($"Abort failed with return {result}");
                    var errorMessage = $"Abort failed because of the following error: {abortErrorMessage}";
                    Log.Error(errorMessage);
                    throw new RpcException(new Status(StatusCode.Internal, errorMessage));
                }
            }
            s_status.UpdateStatus(MicroplateCentrifugeStatus.StatusCode.Idle);
            Log.Debug("CallAbort complete.");
        }
        #endregion

        #region Home
        private void HomeAction(HomeRequest request)
        {
            Log.Debug("HomeAction complete.");
            CallHome();
            Log.Debug("HomeAction complete.");
        }

        private void CallHome()
        {
            Log.Debug("CallHome start.");
            s_status.UpdateStatus(MicroplateCentrifugeStatus.StatusCode.Running);

            int result = s_centrifugeLoader.Home();
            if (result != 0)
            {
                Log.Debug($"Home failed with return {result}");
                HandleCentrifugeError(result);
            }

            s_status.UpdateStatus(MicroplateCentrifugeStatus.StatusCode.Idle);
            Log.Debug("CallHome complete.");
        }
        #endregion

        #region Park
        private void ParkAction(ParkRequest request)
        {
            Log.Debug("ParkAction start.");
            CallPark();
            Log.Debug("ParkAction complete.");
        }

        private void CallPark()
        {
            Log.Debug("CallPark start.");
            s_status.UpdateStatus(MicroplateCentrifugeStatus.StatusCode.Running);

            int result = s_centrifugeLoader.Park();
            if (result != 0)
            {
                Log.Debug($"Park failed with return {result}");
                HandleCentrifugeError(result);
            }

            s_status.UpdateStatus(MicroplateCentrifugeStatus.StatusCode.Idle);
            Log.Debug("CallPark complete.");
        }
        #endregion

        #region LoadPlate
        private void LoadPlateAction(LoadPlateRequest request)
        {
            Log.Debug("LoadPlateAction start.");
            CallLoadPlate(request);
            Log.Debug("LoadPlateAction complete.");
        }

        private void CallLoadPlate(LoadPlateRequest request)
        {
            Log.Debug("CallLoadPlate start.");
            s_status.UpdateStatus(MicroplateCentrifugeStatus.StatusCode.Running);

            int result = s_centrifugeLoader.LoadPlate(request.BucketNumber, request.GripperOffset, request.PlateHeight, request.Speed, request.Options);
            if (result != 0)
            {
                Log.Debug($"LoadPlate failed with return {result}");
                HandleCentrifugeError(result);
            }

            s_status.UpdateStatus(MicroplateCentrifugeStatus.StatusCode.Idle);
            Log.Debug("CallLoadPlate complete.");
        }
        #endregion

        #region UnloadPlate
        private void UnloadPlateAction(UnloadPlateRequest request)
        {
            Log.Debug("UnloadPlateAction start.");
            CallUnloadPlate(request);
            Log.Debug("UnloadPlateAction complete.");
        }

        private void CallUnloadPlate(UnloadPlateRequest request)
        {
            Log.Debug("CallUnloadPlate start.");
            s_status.UpdateStatus(MicroplateCentrifugeStatus.StatusCode.Running);

            int result = s_centrifugeLoader.UnloadPlate(request.BucketNumber, request.GripperOffset, request.PlateHeight, request.Speed, request.Options);
            if (result != 0)
            {
                Log.Debug($"UnloadPlate failed with return {result}");
                HandleCentrifugeError(result);
            }

            s_status.UpdateStatus(MicroplateCentrifugeStatus.StatusCode.Idle);
            Log.Debug("CallUnloadPlate complete.");
        }
        #endregion
    }
}
