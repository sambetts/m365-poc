import '../../App.css';
import { DriveItem } from '@microsoft/microsoft-graph-types';
import { ExampleAppGraphLoader } from '../../services/ExampleAppGraphLoader';
import { useState } from 'react';

interface BreadcrumbItem {
  name: string;
  id: string | null;
}

function OneDriveFiles(props: { files: DriveItem[]; loader: ExampleAppGraphLoader }) {
  const [currentItems, setCurrentItems] = useState<DriveItem[]>(props.files);
  const [breadcrumbs, setBreadcrumbs] = useState<BreadcrumbItem[]>([
    { name: 'Root', id: null }
  ]);
  const [loading, setLoading] = useState(false);

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

  const handleFolderClick = async (folder: DriveItem) => {
    if (!folder.id || !folder.folder) return;
    
    setLoading(true);
    try {
      const items = await props.loader.loadFolderContents(folder.id);
      setCurrentItems(items);
      setBreadcrumbs([...breadcrumbs, { name: folder.name || 'Unknown', id: folder.id }]);
    } catch (error) {
      console.error('Error loading folder:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleBreadcrumbClick = async (index: number) => {
    const clickedBreadcrumb = breadcrumbs[index];
    
    setLoading(true);
    try {
      let items: DriveItem[];
      if (clickedBreadcrumb.id === null) {
        // Root folder
        items = props.files;
      } else {
        items = await props.loader.loadFolderContents(clickedBreadcrumb.id);
      }
      setCurrentItems(items);
      setBreadcrumbs(breadcrumbs.slice(0, index + 1));
    } catch (error) {
      console.error('Error loading folder:', error);
    } finally {
      setLoading(false);
    }
  };

  const getFileIcon = (item: DriveItem): string => {
    if (item.folder) {
      return '📁';
    } else if (item.file) {
      const name = item.name?.toLowerCase() || '';
      if (name.endsWith('.pdf')) return '📄';
      if (name.endsWith('.docx') || name.endsWith('.doc')) return '📝';
      if (name.endsWith('.xlsx') || name.endsWith('.xls')) return '📊';
      if (name.endsWith('.pptx') || name.endsWith('.ppt')) return '📽️';
      if (name.match(/\.(jpg|jpeg|png|gif|bmp)$/)) return '🖼️';
      if (name.match(/\.(mp4|avi|mov|wmv)$/)) return '🎥';
      if (name.match(/\.(mp3|wav|flac)$/)) return '🎵';
      if (name.match(/\.(zip|rar|7z|tar|gz)$/)) return '📦';
      return '📄';
    }
    return '📄';
  };

  return (
    <div style={{ width: '100%' }}>
      {/* Breadcrumb Navigation */}
      <div style={{
        padding: '10px',
        backgroundColor: '#f5f5f5',
        borderRadius: '4px',
        marginBottom: '15px',
        display: 'flex',
        alignItems: 'center',
        flexWrap: 'wrap'
      }}>
        {breadcrumbs.map((crumb, index) => (
          <span key={index} style={{ display: 'flex', alignItems: 'center' }}>
            <button
              onClick={() => handleBreadcrumbClick(index)}
              style={{
                background: 'none',
                border: 'none',
                color: index === breadcrumbs.length - 1 ? '#333' : '#0078d4',
                cursor: index === breadcrumbs.length - 1 ? 'default' : 'pointer',
                fontSize: '14px',
                fontWeight: index === breadcrumbs.length - 1 ? 'bold' : 'normal',
                padding: '4px 8px',
                textDecoration: index === breadcrumbs.length - 1 ? 'none' : 'underline'
              }}
              disabled={index === breadcrumbs.length - 1}
            >
              {crumb.name}
            </button>
            {index < breadcrumbs.length - 1 && (
              <span style={{ margin: '0 5px', color: '#666' }}>›</span>
            )}
          </span>
        ))}
      </div>

      {/* File Browser Table */}
      {loading ? (
        <p>Loading...</p>
      ) : (
        <table style={{
          width: '100%',
          borderCollapse: 'collapse',
          backgroundColor: 'white'
        }}>
          <thead>
            <tr style={{
              backgroundColor: '#f0f0f0',
              borderBottom: '2px solid #ddd'
            }}>
              <th style={{ padding: '12px', textAlign: 'left', width: '50px' }}></th>
              <th style={{ padding: '12px', textAlign: 'left' }}>Name</th>
              <th style={{ padding: '12px', textAlign: 'left', width: '120px' }}>Size</th>
              <th style={{ padding: '12px', textAlign: 'left', width: '150px' }}>Modified</th>
            </tr>
          </thead>
          <tbody>
            {currentItems.length === 0 ? (
              <tr>
                <td colSpan={4} style={{ padding: '20px', textAlign: 'center', color: '#666' }}>
                  This folder is empty
                </td>
              </tr>
            ) : (
              currentItems.map((item: DriveItem) => (
                <tr
                  key={item.id}
                  style={{
                    borderBottom: '1px solid #eee',
                    cursor: item.folder ? 'pointer' : 'default',
                    backgroundColor: 'white'
                  }}
                  onMouseEnter={(e) => {
                    if (item.folder) {
                      e.currentTarget.style.backgroundColor = '#f5f5f5';
                    }
                  }}
                  onMouseLeave={(e) => {
                    e.currentTarget.style.backgroundColor = 'white';
                  }}
                  onClick={() => item.folder && handleFolderClick(item)}
                >
                  <td style={{ padding: '12px', fontSize: '24px' }}>
                    {getFileIcon(item)}
                  </td>
                  <td style={{
                    padding: '12px',
                    fontWeight: item.folder ? 'bold' : 'normal',
                    color: item.folder ? '#0078d4' : '#333'
                  }}>
                    {item.name}
                  </td>
                  <td style={{ padding: '12px', color: '#666' }}>
                    {item.folder ? '—' : formatFileSize(item.size)}
                  </td>
                  <td style={{ padding: '12px', color: '#666' }}>
                    {item.lastModifiedDateTime
                      ? new Date(item.lastModifiedDateTime).toLocaleDateString()
                      : 'N/A'}
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      )}
    </div>
  );
}

export default OneDriveFiles;
