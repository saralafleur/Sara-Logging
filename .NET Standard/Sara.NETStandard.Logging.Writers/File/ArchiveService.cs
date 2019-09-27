using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using Sara.NETStandard.Common.File;

namespace Sara.NETStandard.Logging.Writers.File
{
    internal class ArchiveService
    {
        #region Properties
        public string CurrentDirectory;
        private string _zipFileName;
        public bool IsArchiveSuccess { private set; get; }
        public Exception ArchiveFailException { private set; get; }
        public string FileName;
        public string ArchiveZipSearchPattern;

        private string ZippedFullFileName => Path.Combine(CurrentDirectory, _zipFileName);
        #endregion

        #region Methods
        public void Archive(ArchiveArgs args)
        {
            IsArchiveSuccess = false;
            ArchiveFailException = null;
            _zipFileName = Path.ChangeExtension(FileName, "." + args.Start.ToString(FileConst.CLogFilenameFormat) + "--" + args.End.ToString(FileConst.CLogFilenameFormat) + ".zip");

            try
            {
                CreateArchiveInLoggingFolder(args);
                MoveArchiveToArchiveFolder(Path.Combine(CurrentDirectory,"Archive"));
            }
            catch (Exception ex)
            {
                ArchiveFailException = ex;
            }
        }
        private void CreateArchiveInLoggingFolder(ArchiveArgs args)
        {
            if (System.IO.File.Exists(ZippedFullFileName))
                System.IO.File.Delete(ZippedFullFileName);

            ZipFilesInDateRange(args, ArchiveZipSearchPattern);

            IsArchiveSuccess = true;
        }
        private void MoveArchiveToArchiveFolder(string targetFolder)
        {
            // ReSharper disable once AssignNullToNotNullAttribute
            var targetFullPath = Path.Combine(targetFolder, Path.GetFileName(ZippedFullFileName));

            // if same the return - Sara
            if (targetFullPath == ZippedFullFileName) return;

            if (!Directory.Exists(targetFolder))
                Directory.CreateDirectory(targetFolder);

            if (System.IO.File.Exists(targetFullPath))
                System.IO.File.Delete(targetFullPath);

            System.IO.File.Move(ZippedFullFileName, targetFullPath);
        }
        private DateTime DateTimeFromFile(string filePath)
        {
            try
            {
                var fileName = Path.GetFileNameWithoutExtension(filePath);
                if (fileName == null)
                    throw new NullReferenceException("'fileName' is null.");

                // Check for Multiple Log files for same time - Sara
                if (fileName.Length - fileName.LastIndexOf('_') <= 5)
                    fileName = fileName.Substring(0, fileName.LastIndexOf('_'));


                var lastDotIdx = fileName.LastIndexOf('.');
                if (lastDotIdx == -1)
                    return DateTime.MinValue;

                var strFileDateTime = fileName.Substring(lastDotIdx + 1);
                DateTime fileDateTime;
                if (DateTime.TryParseExact(strFileDateTime, FileConst.CLogFilenameFormat,
                    Thread.CurrentThread.CurrentCulture, DateTimeStyles.None, out fileDateTime))
                    return fileDateTime;
                else
                    return DateTime.MinValue;
            }
            catch (Exception)
            {
                return DateTime.MinValue;
            }
        }
        #endregion

        #region Zip
        public static void ZipAFile(string sourceFullFileName, string destinationFileName, string zippedFullFileName, bool appendToZipFile)
        {
            if (System.IO.File.Exists(zippedFullFileName))
            {
                using (var archive = ZipFile.Open(zippedFullFileName, ZipArchiveMode.Update))
                {
                    archive.CreateEntryFromFile(sourceFullFileName, destinationFileName);
                    //archive.ExtractToDirectory(extractPath);
                }
            }
            else
            {
                using (var fs = new FileStream(zippedFullFileName, FileMode.Create))
                using (var arch = new ZipArchive(fs, ZipArchiveMode.Create))
                    arch.CreateEntryFromFile(sourceFullFileName, destinationFileName);
            }
        }
        private void ZipFilesInDateRange(ArchiveArgs args, string archiveZipSearchPattern)
        {
            var total =
                new List<string>(Directory.GetFiles(CurrentDirectory, archiveZipSearchPattern)).Count();
            var filePathsToZip = new List<string>(Directory.GetFiles(CurrentDirectory, archiveZipSearchPattern))
                .Select(f => new { FilePath = f, FileDateTime = DateTimeFromFile(f) })
                .OrderByDescending(f => f.FileDateTime)
                .Where(f => f.FileDateTime.Date >= args.Start.Date && f.FileDateTime.Date <= args.End)
                .Select(f => f.FilePath);

            var test = new List<string>(Directory.GetFiles(CurrentDirectory, archiveZipSearchPattern))
                .Select(f => new {FilePath = f, FileDateTime = DateTimeFromFile(f)})
                .OrderByDescending(f => f.FileDateTime)
                .Where(f => f.FileDateTime.Date >= args.Start.Date && f.FileDateTime.Date <= args.End)
                .Select(f => f).ToList();

            long bytesZipped = 0;
            int count = 0;
            foreach (var file in filePathsToZip)
            {
                count++;
                if (FileIo.IsFileLocked(new FileInfo(file)))
                    continue;

                ZipAFile(file, Path.GetFileName(file), ZippedFullFileName, true);

                if (ArchiveArgs.IgnoreFileSizeLimits >= args.MaxArchiveSizeBytes) continue;

                var fileInfo = new FileInfo(file);

                bytesZipped += fileInfo.Length;

                if (bytesZipped <= args.MaxArchiveSizeBytes) continue;

                CreateEmptyFileInZip("FileSizeLimitReached.txt", ZippedFullFileName);
                break;
            }

            if (!System.IO.File.Exists(ZippedFullFileName))
            {
                CreateEmptyFileInZip("noLogs.txt", ZippedFullFileName);
            }

        }
        private void CreateEmptyFileInZip(string emptyFileName, string fullZipFileName)
        {
            var fullEmptyFileName = Path.Combine(CurrentDirectory, emptyFileName);
            using (System.IO.File.Create(fullEmptyFileName)) { } //Make sure the stream gets closed
            ZipAFile(fullEmptyFileName, Path.GetFileName(fullEmptyFileName), fullZipFileName, true);
        }
        #endregion
    }
}
