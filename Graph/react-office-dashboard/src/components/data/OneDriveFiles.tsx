import '../../App.css';
import { DriveItem } from '@microsoft/microsoft-graph-types';

function OneDriveFiles(props: { files: DriveItem[] }) {

  const formatFileSize = (bytes: number | null | undefined): string => {
    if (!bytes) return 'N/A';
    const kb = bytes / 1024;
    const mb = kb / 1024;
    
    if (mb >= 1) {
      return `${mb.toFixed(2)} MB`;
    } else if (kb >= 1) {
      return `${kb.toFixed(2)} KB`;
    } else {
      return `${bytes} bytes`;
    }
  };

  return (
    <>
      <ul>
        {props.files.map((file: DriveItem) => {
          return (
            <li key={file.id}>
              <button type="button" style={{ 
                background: 'none', 
                border: 'none', 
                padding: 0, 
                font: 'inherit', 
                cursor: 'pointer',
                color: 'inherit',
                textAlign: 'left',
                width: '100%'
              }}>
                <strong>{file.name}</strong>
                <span className="email-details">
                  Size: {formatFileSize(file.size)} | Modified: {file.lastModifiedDateTime ? new Date(file.lastModifiedDateTime).toLocaleDateString() : 'N/A'}
                </span>
              </button>
            </li>
          );
        })}
      </ul>
    </>
  );
}

export default OneDriveFiles;
