import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { documentService } from '../services/api';
import { Document } from '../types';
import './Dashboard.css';

const Dashboard: React.FC = () => {
  const [documents, setDocuments] = useState<Document[]>([]);
  const [selectedFiles, setSelectedFiles] = useState<File[]>([]);
  const [selectedDocIds, setSelectedDocIds] = useState<number[]>([]);
  const [loading, setLoading] = useState(false);
  const [uploading, setUploading] = useState(false);
  const [analyzing, setAnalyzing] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  useEffect(() => {
    fetchDocuments();
  }, []);

  const fetchDocuments = async () => {
    setLoading(true);
    try {
      const docs = await documentService.getDocuments();
      setDocuments(docs);
    } catch (err) {
      setError('Failed to load documents');
    } finally {
      setLoading(false);
    }
  };

  const handleFileSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files) {
      const files = Array.from(e.target.files);

      // Validate file types
      const validTypes = ['application/pdf', 'application/vnd.openxmlformats-officedocument.wordprocessingml.document', 'application/msword'];
      const invalidFiles = files.filter(f => !validTypes.includes(f.type));

      if (invalidFiles.length > 0) {
        setError('Only PDF and Word documents are allowed');
        return;
      }

      // Check total count
      if (documents.length + files.length > 10) {
        setError('Maximum of 10 documents allowed per user');
        return;
      }

      setSelectedFiles(files);
      setError('');
    }
  };

  const handleUpload = async () => {
    if (selectedFiles.length === 0) return;

    setUploading(true);
    setError('');
    setSuccess('');

    try {
      for (const file of selectedFiles) {
        await documentService.uploadDocument(file);
      }
      setSuccess(`Successfully uploaded ${selectedFiles.length} file(s)`);
      setSelectedFiles([]);
      fetchDocuments();
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to upload documents');
    } finally {
      setUploading(false);
    }
  };

  const handleDocumentSelect = (id: number) => {
    setSelectedDocIds(prev =>
      prev.includes(id) ? prev.filter(docId => docId !== id) : [...prev, id]
    );
  };

  const handleAnalyze = async () => {
    if (selectedDocIds.length === 0) {
      setError('Please select at least one document to analyze');
      return;
    }

    setAnalyzing(true);
    setError('');
    setSuccess('');

    try {
      await documentService.analyzeDocuments(selectedDocIds);
      setSuccess('Analysis completed successfully!');
      setSelectedDocIds([]);
      fetchDocuments();
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to analyze documents');
    } finally {
      setAnalyzing(false);
    }
  };

  const handleDelete = async (id: number) => {
    if (!window.confirm('Are you sure you want to delete this document?')) return;

    try {
      await documentService.deleteDocument(id);
      setSuccess('Document deleted successfully');
      fetchDocuments();
    } catch (err) {
      setError('Failed to delete document');
    }
  };

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  const formatFileSize = (bytes: number) => {
    if (bytes < 1024) return bytes + ' B';
    if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(2) + ' KB';
    return (bytes / (1024 * 1024)).toFixed(2) + ' MB';
  };

  return (
    <div className="dashboard-container">
      <header className="dashboard-header">
        <h1>Document Analysis Dashboard</h1>
        <div className="user-info">
          <span>{user?.email}</span>
          <button onClick={handleLogout} className="btn-secondary">Logout</button>
        </div>
      </header>

      <div className="dashboard-content">
        {error && <div className="error-message">{error}</div>}
        {success && <div className="success-message">{success}</div>}

        <div className="upload-section">
          <h2>Upload Documents ({documents.length}/10)</h2>
          <div className="upload-controls">
            <input
              type="file"
              multiple
              accept=".pdf,.doc,.docx"
              onChange={handleFileSelect}
              disabled={uploading || documents.length >= 10}
            />
            {selectedFiles.length > 0 && (
              <div className="selected-files">
                <p>Selected: {selectedFiles.map(f => f.name).join(', ')}</p>
                <button
                  onClick={handleUpload}
                  disabled={uploading}
                  className="btn-primary"
                >
                  {uploading ? 'Uploading...' : 'Upload'}
                </button>
              </div>
            )}
          </div>
        </div>

        <div className="analyze-section">
          <button
            onClick={handleAnalyze}
            disabled={analyzing || selectedDocIds.length === 0}
            className="btn-primary"
          >
            {analyzing ? 'Analyzing...' : `Analyze Selected (${selectedDocIds.length})`}
          </button>
        </div>

        <div className="documents-section">
          <h2>Your Documents</h2>
          {loading ? (
            <p>Loading documents...</p>
          ) : documents.length === 0 ? (
            <p>No documents uploaded yet.</p>
          ) : (
            <div className="documents-grid">
              {documents.map(doc => (
                <div key={doc.id} className={`document-card ${selectedDocIds.includes(doc.id) ? 'selected' : ''}`}>
                  <div className="document-header">
                    <input
                      type="checkbox"
                      checked={selectedDocIds.includes(doc.id)}
                      onChange={() => handleDocumentSelect(doc.id)}
                    />
                    <h3>{doc.fileName}</h3>
                  </div>
                  <p className="document-meta">
                    Size: {formatFileSize(doc.fileSize)} |
                    Uploaded: {new Date(doc.uploadedAt).toLocaleDateString()}
                  </p>

                  {doc.analysis ? (
                    <div className="analysis-results">
                      <h4>Analysis Results</h4>
                      {doc.analysis.projectName && (
                        <div className="analysis-item">
                          <strong>Project Name:</strong> {doc.analysis.projectName}
                        </div>
                      )}
                      {doc.analysis.projectDuration && (
                        <div className="analysis-item">
                          <strong>Duration:</strong> {doc.analysis.projectDuration}
                        </div>
                      )}
                      {doc.analysis.humanResourcesHierarchy && (
                        <div className="analysis-item">
                          <strong>HR Hierarchy:</strong> {doc.analysis.humanResourcesHierarchy}
                        </div>
                      )}
                      {doc.analysis.projectStages && (
                        <div className="analysis-item">
                          <strong>Stages:</strong> {doc.analysis.projectStages}
                        </div>
                      )}
                      {doc.analysis.specialConditions && (
                        <div className="analysis-item">
                          <strong>Special Conditions:</strong> {doc.analysis.specialConditions}
                        </div>
                      )}
                      {doc.analysis.implementationBoundaries && (
                        <div className="analysis-item">
                          <strong>Implementation Boundaries:</strong> {doc.analysis.implementationBoundaries}
                        </div>
                      )}
                    </div>
                  ) : (
                    <p className="no-analysis">Not analyzed yet</p>
                  )}

                  <button
                    onClick={() => handleDelete(doc.id)}
                    className="btn-danger"
                  >
                    Delete
                  </button>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

export default Dashboard;
